using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CatIdleExpressions : MonoBehaviour
{
    public Sprite awakeBody;
    public Sprite sleepingBody;
    public Sprite[] faces;
    public Image faceOverlay;
    private Image body;
    private Coroutine idleRoutine;
    private int expression;

    private void OnEnable()
    {
        body = GetComponent<Image>();
        ShowExpression(0);
        if (faces != null && faces.Length > 0) idleRoutine = StartCoroutine(Idle());
    }

    private void OnDisable()
    {
        if (idleRoutine != null) StopCoroutine(idleRoutine);
        idleRoutine = null;
    }

    private IEnumerator Idle()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(expression == faces.Length
                ? UnityEngine.Random.Range(10f, 15f) : UnityEngine.Random.Range(5f, 9f));
            if (faces == null || faces.Length == 0) continue;
            // Select a different expression each time, including the sleeping pose.
            int next = UnityEngine.Random.Range(0, faces.Length);
            if (next >= expression) next++;
            ShowExpression(next);
        }
    }

    public void ShowExpression(int index)
    {
        if (body == null) body = GetComponent<Image>();
        if (faces == null || faces.Length == 0 || awakeBody == null || sleepingBody == null || faceOverlay == null) return;
        expression = Mathf.Clamp(index, 0, faces.Length);
        bool sleeping = expression == faces.Length;
        body.sprite = sleeping ? sleepingBody : awakeBody;
        body.preserveAspect = true;
        faceOverlay.gameObject.SetActive(!sleeping);
        if (!sleeping)
        {
            faceOverlay.sprite = faces[expression];
            LayoutFace();
        }
    }

    private void LateUpdate()
    {
        if (faceOverlay != null && faceOverlay.gameObject.activeSelf) LayoutFace();
    }

    private void LayoutFace()
    {
        if (awakeBody == null || faceOverlay.sprite == null) return;
        // Align the replacement face with the head in the full-body source artwork.
        Rect area = body.rectTransform.rect;
        float scale = Mathf.Min(area.width / awakeBody.rect.width, area.height / awakeBody.rect.height);
        var rect = faceOverlay.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 1f);
        Vector2 renderedSize = awakeBody.rect.size * scale;
        Vector2 alignment = Vector2.Scale(area.size - renderedSize, body.rectTransform.pivot - new Vector2(0.5f, 0.5f));
        rect.anchoredPosition = alignment + new Vector2(0, renderedSize.y * 0.5f);
        rect.sizeDelta = faceOverlay.sprite.rect.size * scale;
    }
}
