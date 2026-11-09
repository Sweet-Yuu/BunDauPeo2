using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hoang.Remake.EditorTools
{
    public static class SkillTreeLayoutTool
    {
        [MenuItem("Tools/Hoang/Skill Tree/Validate selected graph")]
        public static void ValidateSelected()
        {
            var ui = SelectedGraph();
            if (ui == null) return;
            if (Validate(ui)) EditorUtility.DisplayDialog("Skill tree", "The selected graph is valid.", "OK");
        }

        [MenuItem("Tools/Hoang/Skill Tree/Arrange selected graph")]
        public static void ArrangeSelected()
        {
            var ui = SelectedGraph();
            if (ui == null || !Validate(ui)) return;
            var objects = new List<UnityEngine.Object> { ui };
            foreach (var slot in ui.Slots) if (slot != null) objects.Add(slot.transform);
            Undo.RecordObjects(objects.ToArray(), "Arrange restaurant skill tree");
            ui.ArrangeGraph();
            EditorUtility.SetDirty(ui);
            if (ui.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }

        private static SkillTreeUI SelectedGraph()
        {
            var selected = Selection.activeGameObject;
            var ui = selected == null ? null : selected.GetComponentInParent<SkillTreeUI>(true);
            if (ui == null && selected != null) ui = selected.GetComponentInChildren<SkillTreeUI>(true);
            if (ui == null) EditorUtility.DisplayDialog("Skill tree", "Select a SkillTree or its Canvas first.", "OK");
            return ui;
        }

        private static bool Validate(SkillTreeUI ui)
        {
            var definitions = new List<SkillSO>();
            foreach (var slot in ui.Slots) if (slot != null) definitions.Add(slot.Definition);
            try
            {
                using var manager = new SkillTreeManager(new RemakeContext(new HoangSaveData(), _ => true), definitions.ToArray());
                return true;
            }
            catch (ArgumentException exception)
            {
                EditorUtility.DisplayDialog("Invalid skill tree", exception.Message, "OK");
                return false;
            }
        }
    }
}
