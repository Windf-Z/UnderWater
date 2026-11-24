#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnderwaterOptimization
{
    public static class UnderwaterColliders
    {
        private const float MeshColliderThreshold = 12f; // 任一边超过这个尺寸就用 MeshCollider

        [MenuItem("Tools/Underwater/6. Add Colliders (舱内道具)", false, 6)]
        public static void AddCollidersMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            string report = AddAll();
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

        }

        public static string AddAll()
        {
            int boxes = 0;
            int meshes = 0;
            int skipped = 0;
            StringBuilder sb = new StringBuilder("[Underwater] 补碰撞体：");

            foreach (MeshRenderer r in Object.FindObjectsOfType<MeshRenderer>())
            {
                if (r is ParticleSystemRenderer)
                    continue;

                GameObject go = r.gameObject;

                if (go.name == "WaterBlock_50m" || go.name.StartsWith("Underwater_"))
                {
                    skipped++;
                    continue;
                }

                if (go.GetComponentInParent<Collider>(true) != null)
                {
                    skipped++;
                    continue;
                }

                MeshFilter filter = go.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    skipped++;
                    continue;
                }

                Bounds bounds = filter.sharedMesh.bounds;
                float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

                if (maxSize > MeshColliderThreshold)
                {
                    MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(go);
                    meshCollider.sharedMesh = filter.sharedMesh;
                    meshCollider.convex = false;
                    meshes++;
                }
                else
                {
                    BoxCollider box = Undo.AddComponent<BoxCollider>(go);
                    box.center = bounds.center;
                    box.size = bounds.size;
                    boxes++;
                }
            }

            sb.Append("BoxCollider ").Append(boxes).Append(" 个，MeshCollider ").Append(meshes)
              .Append(" 个，跳过 ").Append(skipped).Append(" 个。");
            sb.Append("（大件用 MeshCollider，小件用 BoxCollider；已有碰撞的不会重复加）");
            return sb.ToString();
        }
    }
}
#endif