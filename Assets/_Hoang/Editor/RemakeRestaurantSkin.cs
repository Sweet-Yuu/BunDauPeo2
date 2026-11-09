using UnityEditor;

namespace Hoang.Remake.Editor
{
    public static class RemakeRestaurantSkin
    {
        public static void Apply(RemakeShell shell)
        {
            RemakeRestaurantTheme.Apply(shell);
            foreach (var component in shell.GetComponentsInChildren<UnityEngine.Component>(true))
                if (component != null) EditorUtility.SetDirty(component);
        }
    }
}
