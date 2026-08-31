using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
internal static class ScriptingDefineSanitizer
{
    private const string ProjectDefine = "DATN2D_PROJECT";

    static ScriptingDefineSanitizer()
    {
        EditorApplication.delayCall += SanitizeActiveBuildTarget;
    }

    private static void SanitizeActiveBuildTarget()
    {
        NamedBuildTarget namedTarget =
            NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );

        string rawDefines =
            PlayerSettings.GetScriptingDefineSymbols(namedTarget);

        string[] splitDefines =
            (rawDefines ?? string.Empty).Split(';');

        List<string> cleanDefines = new List<string>();
        HashSet<string> uniqueDefines =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (string define in splitDefines)
        {
            string cleanDefine = define.Trim();

            if (cleanDefine.Length == 0 ||
                !uniqueDefines.Add(cleanDefine))
            {
                continue;
            }

            cleanDefines.Add(cleanDefine);
        }

        if (uniqueDefines.Add(ProjectDefine))
            cleanDefines.Add(ProjectDefine);

        string sanitizedDefines =
            string.Join(";", cleanDefines);

        if (string.Equals(
                rawDefines,
                sanitizedDefines,
                StringComparison.Ordinal
            ))
        {
            return;
        }

        PlayerSettings.SetScriptingDefineSymbols(
            namedTarget,
            sanitizedDefines
        );

        Debug.Log(
            "Removed empty or duplicate scripting define symbols " +
            $"for {namedTarget.TargetName}."
        );
    }
}
