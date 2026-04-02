// Assets/Editor/SignatureExporter.cs
// Unity 에디터 메뉴 Tools > Export Signatures 로 실행
// 프로젝트 루트에 .ai-signatures/ 폴더를 생성하고
// Assets/Scripts/ 의 폴더 구조를 그대로 미러링하여 시그니처 파일을 출력합니다.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SignatureExporter
{
    private const string OutputDir = ".ai-signatures";
    private const string ScriptsDir = "Assets/Scripts";

    [MenuItem("Tools/Export Signatures")]
    public static void Export()
    {
        var projectPath = Path.GetDirectoryName(Application.dataPath);
        var outDir = Path.Combine(projectPath, OutputDir);
        var scriptsFullPath = Path.Combine(projectPath, ScriptsDir);

        if (!Directory.Exists(scriptsFullPath))
        {
            Debug.LogError($"[SignatureExporter] {ScriptsDir} 폴더가 존재하지 않습니다.");
            return;
        }

        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        var scripts = Directory.GetFiles(scriptsFullPath, "*.cs", SearchOption.AllDirectories);
        int exportedCount = 0;

        foreach (var script in scripts)
        {
            // Assets/Scripts/ 기준 상대경로 계산
            var relativeToScripts = script
                .Substring(scriptsFullPath.Length + 1)
                .Replace(Path.DirectorySeparatorChar, '/');

            // 출력 경로: .ai-signatures/Entity/Player.sig.cs
            var sigRelativePath = Path.ChangeExtension(relativeToScripts, ".sig.cs");
            var sigFullPath = Path.Combine(outDir, sigRelativePath);

            // Assets/Scripts 기준 상대경로 (Source 주석용)
            var sourceRelative = ScriptsDir + "/" + relativeToScripts;

            try
            {
                var content = File.ReadAllText(script);
                var signature = ExtractSignature(content);

                if (string.IsNullOrWhiteSpace(signature))
                    continue;

                var sigDir = Path.GetDirectoryName(sigFullPath);
                if (!Directory.Exists(sigDir))
                    Directory.CreateDirectory(sigDir);

                var sb = new StringBuilder();
                sb.AppendLine($"// Source: {sourceRelative}");
                sb.AppendLine(signature);

                File.WriteAllText(sigFullPath, sb.ToString(), new System.Text.UTF8Encoding(true));
                exportedCount++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SignatureExporter] Skipped {sourceRelative}: {e.Message}");
            }
        }

        Debug.Log($"[SignatureExporter] Exported {exportedCount} file(s) to {OutputDir}/");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 소스 코드에서 시그니처만 추출. 메서드/프로퍼티 본문은 제거하고 주석은 보존.
    /// </summary>
    static string ExtractSignature(string source)
    {
        var lines = source.Split('\n');
        var sb = new StringBuilder();
        int braceDepth = 0;
        bool insideMethodBody = false;
        int methodBodyStartDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            // using 문 보존
            if (braceDepth == 0 && trimmed.StartsWith("using ") && trimmed.Contains(';'))
            {
                sb.AppendLine(line);
                continue;
            }

            // 메서드 본문 내부 → 스킵하되 brace depth 추적
            if (insideMethodBody)
            {
                foreach (char c in line)
                {
                    if (c == '{') braceDepth++;
                    else if (c == '}')
                    {
                        braceDepth--;
                        if (braceDepth <= methodBodyStartDepth)
                        {
                            insideMethodBody = false;
                            sb.AppendLine(new string(' ', methodBodyStartDepth * 4) + "}");
                            break;
                        }
                    }
                }
                continue;
            }

            // 메서드/본문이 있는 프로퍼티 감지
            bool isMethodLike = IsMethodOrPropertyWithBody(trimmed, braceDepth);

            // 이 줄의 brace depth 변화 계산
            int openBraces = CountChar(line, '{');
            int closeBraces = CountChar(line, '}');

            if (isMethodLike && (trimmed.Contains('{') || LookAheadForBrace(lines, i)))
            {
                // 시그니처 부분만 출력
                var sigPart = ExtractMethodSignatureLine(line);
                sb.AppendLine(sigPart + " { /* ... */ }");

                braceDepth += openBraces - closeBraces;

                // 이 줄에서 brace가 열린 채 끝났으면 본문 스킵 모드
                if (openBraces > closeBraces)
                {
                    insideMethodBody = true;
                    methodBodyStartDepth = braceDepth - 1;
                }
                continue;
            }

            // 람다 본문 프로퍼티/메서드 (=> expr;)
            if (isMethodLike && trimmed.Contains("=>"))
            {
                var arrowIdx = line.IndexOf("=>");
                sb.AppendLine(line.Substring(0, arrowIdx) + "=> /* ... */;");
                braceDepth += openBraces - closeBraces;
                continue;
            }

            braceDepth += openBraces - closeBraces;
            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    static bool IsMethodOrPropertyWithBody(string trimmed, int braceDepth)
    {
        if (braceDepth < 1) return false;
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") ||
            trimmed.StartsWith("*") || trimmed.StartsWith("///"))
            return false;
        if (trimmed.StartsWith("[")) return false;

        // 클래스/구조체/인터페이스/enum 선언은 제외
        string[] typeKeywords = {
            "class ", "struct ", "interface ", "enum ",
            "public class", "public struct", "public interface", "public enum",
            "private class", "internal class", "protected class",
            "abstract class", "sealed class", "static class",
            "public abstract class", "public sealed class", "public static class"
        };
        foreach (var kw in typeKeywords)
        {
            if (trimmed.StartsWith(kw)) return false;
        }

        // 제어문 제외
        string[] controlKeywords = {
            "if ", "if(", "for ", "for(", "foreach ", "foreach(",
            "while ", "while(", "switch ", "switch(",
            "catch ", "catch(", "lock ", "lock(",
            "else", "try", "finally", "return ", "throw ", "yield "
        };
        foreach (var kw in controlKeywords)
        {
            if (trimmed.StartsWith(kw)) return false;
        }

        // 메서드 패턴: 괄호가 있으면 메서드일 가능성 높음
        if (trimmed.Contains('('))
            return true;

        // expression-bodied member: => 포함
        if (trimmed.Contains("=>") && !trimmed.StartsWith("="))
            return true;

        return false;
    }

    static bool LookAheadForBrace(string[] lines, int current)
    {
        for (int j = current + 1; j < Math.Min(current + 3, lines.Length); j++)
        {
            var t = lines[j].TrimStart();
            if (t.StartsWith("{")) return true;
            if (t.Length > 0) break;
        }
        return false;
    }

    static string ExtractMethodSignatureLine(string line)
    {
        var idx = line.IndexOf('{');
        if (idx >= 0) return line.Substring(0, idx).TrimEnd();

        idx = line.IndexOf("=>");
        if (idx >= 0) return line.Substring(0, idx).TrimEnd();

        return line.TrimEnd();
    }

    static int CountChar(string s, char c)
    {
        int count = 0;
        foreach (var ch in s)
            if (ch == c) count++;
        return count;
    }
}