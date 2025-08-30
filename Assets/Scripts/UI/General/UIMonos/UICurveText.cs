using UnityEngine;
using TMPro;

namespace SparFlame.UI.General
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class UICurveText : MonoBehaviour
    {
        private TMP_Text _textComponent;

        [Header("Curve Settings")] public AnimationCurve vertexCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.25f, 2.0f),
            new Keyframe(0.5f, 0),
            new Keyframe(0.75f, 2.0f),
            new Keyframe(1, 0f)
        );

        public float curveScale = 10f;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            WrapText(); // 初始排版
        }

        /// <summary>
        /// 对当前文本执行一次 wrap
        /// </summary>
        public void WrapText()
        {
            if (!_textComponent) return;

            _textComponent.ForceMeshUpdate();

            var textInfo = _textComponent.textInfo;
            var characterCount = textInfo.characterCount;
            if (characterCount == 0) return;

            var boundsMinX = _textComponent.bounds.min.x;
            var boundsMaxX = _textComponent.bounds.max.x;

            for (var i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                var vertexIndex = textInfo.characterInfo[i].vertexIndex;
                var materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                var vertices = textInfo.meshInfo[materialIndex].vertices;

                // 中心点
                var offsetToMidBaseline = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2;
                offsetToMidBaseline.y = textInfo.characterInfo[i].baseLine;

                // 平移到原点
                for (var j = 0; j < 4; j++)
                    vertices[vertexIndex + j] -= offsetToMidBaseline;

                // 计算曲线
                var x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
                var x1 = x0 + 0.0001f;
                var y0 = vertexCurve.Evaluate(x0) * curveScale;
                var y1 = vertexCurve.Evaluate(x1) * curveScale;

                var tangent = new Vector3((x1 * (boundsMaxX - boundsMinX) + boundsMinX), y1)
                              - new Vector3(offsetToMidBaseline.x, y0);

                var angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

                var matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                for (var j = 0; j < 4; j++)
                    vertices[vertexIndex + j] = matrix.MultiplyPoint3x4(vertices[vertexIndex + j]);

                // 移回去
                for (var j = 0; j < 4; j++)
                    vertices[vertexIndex + j] += offsetToMidBaseline;
            }

            // 应用 mesh 更新
            _textComponent.UpdateVertexData();
        }
    }
}