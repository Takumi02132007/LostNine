using UnityEngine;
using UnityEngine.UI;

public class BackgroundBlur : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RawImage blurImage;
    [SerializeField] private Material blurMaterial; // GaussianBlurなど

    private RenderTexture _rt;

    private void OnEnable()
    {
        if (_rt == null)
            _rt = new RenderTexture(Screen.width, Screen.height, 0);

        targetCamera.targetTexture = _rt;
        targetCamera.Render();

        blurMaterial.SetFloat("_Size", 4f); // ぼかし強度

        Graphics.Blit(_rt, _rt, blurMaterial);
        blurImage.texture = _rt;

        targetCamera.targetTexture = null;
    }
}
