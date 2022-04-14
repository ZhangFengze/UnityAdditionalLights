using UnityEngine;
using UnityEditor;

public static class ViewLightIntersection
{
    [MenuItem("ZFZ/Draw Mode: Light Overlap")]
    static void SceneViewCustomSceneMode()
    {
        SceneView.lastActiveSceneView.SetSceneViewShaderReplace(Shader.Find("Light Overlap"), null);
    }

    [MenuItem("ZFZ/Draw Mode: None")]
    static void SceneViewClearSceneView()
    {
        foreach (SceneView sceneView in SceneView.sceneViews)
            sceneView.SetSceneViewShaderReplace(null, null);
    }
}
