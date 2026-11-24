using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterOptimization
{
    public class PowerGridPanel : MonoBehaviour
    {
        public List<PowerTerminal> terminals = new List<PowerTerminal>();
        public float workRange = 5f;
        public float pickAngle = 35f;

        private UnderwaterFlow flow;
        private PowerTerminal selected;
        private int connected;
        private LineRenderer preview;
        private bool nearPanel;

        public bool IsPowered
        {
            get { return terminals.Count > 0 && connected >= terminals.Count / 2; }
        }

        private void Start()
        {
            flow = FindObjectOfType<UnderwaterFlow>();

            GameObject go = new GameObject("GridPreviewWire");
            preview = go.AddComponent<LineRenderer>();
            preview.positionCount = 2;
            preview.useWorldSpace = true;
            preview.widthMultiplier = 0.05f;
            preview.enabled = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.SetColor("_BaseColor", Color.white);
                preview.material = material;
            }

            // 兜底：场景里没配列表时自动收集
            if (terminals.Count == 0)
            {
                PowerTerminal[] found = FindObjectsOfType<PowerTerminal>(true);
                terminals.AddRange(found);
            }

        }

        private void Update()
        {
            if (IsPowered)
                return;

            Vector3 origin = UnderwaterInput.RayOrigin;
            Vector3 direction = UnderwaterInput.RayDirection;

            nearPanel = Vector3.Distance(origin, transform.position) <= workRange;

            if (!nearPanel)
            {
                if (selected != null)
                    Cancel("离开接线板，线放回去了");

                nearPanel = false;
                return;
            }

            // 空手：按住左键抓最近的一根线
            if (selected == null && UnderwaterInput.FireDown)
            {
                selected = FindNearest(direction, origin, true);

                if (selected != null)
                {
                    selected.SetDragging(true);

                    if (preview.material != null)
                        preview.material.SetColor("_BaseColor", PowerTerminal.ColorOf(selected.colorIndex));

                }
            }

            // 抓着手：线实时跟准星，松开就接
            if (selected != null)
            {
                Vector3 tip = origin + direction * 4f;
                preview.enabled = true;
                preview.SetPosition(0, selected.transform.position);
                preview.SetPosition(1, tip);

                if (UnderwaterInput.FireUp)
                    Release();
            }
            else
            {
                preview.enabled = false;
            }
        }

        // 提示放在 LateUpdate，盖过电话/绳索枪每帧刷新的文字
        private void LateUpdate()
        {
            if (flow == null || IsPowered || !nearPanel)
                return;

            if (selected != null)
            {
                PowerTerminal partner = FindPartner(selected);
                flow.SetHint(partner != null
                    ? "拖着" + PowerTerminal.NameOf(selected.colorIndex) + "的线 —— 松开就接到另一个端子上"
                    : "把线拖到空处松开？");
            }
            else
            {
                flow.SetHint("请抓取电线（" + connected + "/" + (terminals.Count / 2) + " 已接）");
            }
        }

        private PowerTerminal FindNearest(Vector3 direction, Vector3 origin, bool requireUnwired)
        {
            PowerTerminal best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < terminals.Count; i++)
            {
                PowerTerminal terminal = terminals[i];

                if (terminal == null)
                    continue;

                if (requireUnwired && terminal.wired)
                    continue;

                Vector3 to = terminal.transform.position - origin;
                float distance = to.magnitude;

                if (distance > workRange)
                    continue;

                float angle = Vector3.Angle(direction, to);

                // 角度优先，其次距离
                float score = angle * 2f + distance;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = terminal;
                }
            }

            return best;
        }

        private PowerTerminal FindPartner(PowerTerminal terminal)
        {
            for (int i = 0; i < terminals.Count; i++)
            {
                PowerTerminal other = terminals[i];

                if (other == null || other == terminal)
                    continue;

                if (!other.wired && other.colorIndex == terminal.colorIndex)
                    return other;
            }

            return null;
        }

        private void Release()
        {
            PowerTerminal partner = FindPartner(selected);

            if (partner == null)
            {
                Cancel("没接上 —— 这根线已经接好过了");
                return;
            }

            Connect(selected, partner);
            selected = null;
        }

        private void Connect(PowerTerminal a, PowerTerminal b)
        {
            a.SetWired(true);
            b.SetWired(true);
            a.SetDragging(false);
            b.SetDragging(false);
            connected++;

            GameObject wire = new GameObject("Wire");
            LineRenderer line = wire.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, a.transform.position);
            line.SetPosition(1, b.transform.position);
            line.widthMultiplier = 0.05f;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.SetColor("_BaseColor", PowerTerminal.ColorOf(a.colorIndex));
                line.material = material;
            }

            preview.enabled = false;

            if (IsPowered)
            {
                if (flow != null)
                {
                    flow.SetHint("电路接好了 —— 水开始漫进来，快去打电话");
                    flow.OnGridPowered();
                }
            }
            else if (flow != null)
            {
                flow.SetHint("接好 " + connected + "/" + (terminals.Count / 2) + " 组");
            }
        }

        private void Cancel(string message)
        {
            if (selected != null)
                selected.SetDragging(false);

            selected = null;

            if (preview != null)
                preview.enabled = false;

            if (flow != null)
                flow.SetHint(message);
        }
    }
}