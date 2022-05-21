using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour
{
    public Canvas canvas;
    public Texture2DArray items;
    public Texture2DArray blocks;
    public Texture2D icons;
    public RawImage itemSelectImage;
    public int moveWidth = 80;
    [Range(1,9)]
    public int selectedPos = 1;

    private string[] buttons = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    public int[] itemBarIds = new int[9];

    // Start is called before the first frame update
    void Start()
    {
        SetSelectedItemPosition(selectedPos);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (string button in buttons)
        {
            if (Input.GetKeyDown(button))
            {
                selectedPos = int.Parse(button);
            }
        }
        SetSelectedItemPosition(selectedPos);

        if (Input.GetKeyDown("r"))
        {
        }

        if (Input.GetKeyDown("f1"))
        {
            ToggleUI();
        }
    }

    public void SetSelectedItemPosition(int pos)
    {
        int x = (pos - 5) * moveWidth;
        itemSelectImage.rectTransform.anchoredPosition = new Vector2(x, itemSelectImage.rectTransform.position.y);
    }

    public void InitItems()
    {
        GameObject itemBar = canvas.transform.Find("ItemBar").gameObject;
        for (int i = 1; i <= 9; i++)
        {
            GameObject item = new GameObject(i.ToString());
            item.SetActive(false);
            RectTransform rectTransform = item.AddComponent<RectTransform>();
            item.transform.SetParent(itemBar.transform);
            item.AddComponent<CanvasRenderer>();
            item.AddComponent<RawImage>();

            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = new Vector2((i - 5) * moveWidth, 44);
            rectTransform.sizeDelta = new Vector2(64, 64);
        }
        itemBarIds[0] = 3;
        itemBarIds[1] = 1;
        itemBarIds[2] = 16;
        SetItemAtPos(1, 3);
        SetItemAtPos(2, 1);
        SetItemAtPos(3, 16);
    }

    public void SetItemAtPos(int pos, int itemId)
    {
        GameObject itemBar = canvas.transform.Find("ItemBar").gameObject;
        GameObject item = itemBar.transform.Find(pos.ToString()).gameObject;
        if (itemId == -1)
        {
            item.SetActive(false);
            return;
        }
        else
        {
            item.SetActive(true);
        }

        Color32[] pixels = blocks.GetPixels32(itemId, 0);
        Texture2D texture2D = new Texture2D(blocks.width, blocks.height);
        texture2D.filterMode = FilterMode.Point;
        texture2D.wrapMode = TextureWrapMode.Clamp;
        texture2D.SetPixels32(pixels);
        texture2D.Apply();
        item.GetComponent<RawImage>().texture = texture2D;
    }

    public void InitHealth()
    {
        GameObject healthBar = canvas.transform.Find("HealthBar").gameObject;
        for (int i = 1; i <= 10; i++)
        {
            GameObject image = new GameObject(i.ToString());
            image.transform.parent = healthBar.transform;
            RectTransform rectTransform = image.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector3(i*21-10, 0, 0);
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.sizeDelta = new Vector2(20, 20);
            image.AddComponent<RawImage>();
        }
        SetHealth(20);
    }

    public void SetHealth(int health)
    {
        GameObject healthBar = canvas.transform.Find("HealthBar").gameObject;
        int childCount = healthBar.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            RawImage rawImage = healthBar.transform.GetChild(i).gameObject.GetComponent<RawImage>();
            rawImage.texture = icons;
            rawImage.uvRect = new Rect(105f / 512f, 1f - ((15f+1f) / 512f), (119f-105f+1f) / 512f, (15f-2f+1f) / 512f);
        }
    }

    public void InitHunger()
    {
        GameObject hungerBar = canvas.transform.Find("HungerBar").gameObject;
        for (int i = 1; i <= 10; i++)
        {
            GameObject image = new GameObject(i.ToString());
            image.transform.parent = hungerBar.transform;
            RectTransform rectTransform = image.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector3(-(i * 21) + 10, 0, 0);
            rectTransform.anchorMin = new Vector2(1, 0.5f);
            rectTransform.anchorMax = new Vector2(1, 0.5f);
            rectTransform.sizeDelta = new Vector2(20, 20);
            image.AddComponent<RawImage>();
        }
        SetHunger(20);
    }

    public void SetHunger(int hunger)
    {
        GameObject hungerBar = canvas.transform.Find("HungerBar").gameObject;
        int childCount = hungerBar.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            RawImage rawImage = hungerBar.transform.GetChild(i).gameObject.GetComponent<RawImage>();
            rawImage.texture = icons;
            rawImage.uvRect = new Rect(105f / 512f, 1f - ((70f + 1f) / 512f), (120f - 105f + 1f) / 512f, (70f - 55f + 1f) / 512f);
        }
    }

    void ToggleUI()
    {
        if (canvas.transform.Find("Crosshair").gameObject.activeInHierarchy)
        {
            canvas.transform.Find("Crosshair").gameObject.SetActive(false);
            canvas.transform.Find("ItemBar").gameObject.SetActive(false);
            canvas.transform.Find("ItemSelect").gameObject.SetActive(false);
            canvas.transform.Find("HealthBar").gameObject.SetActive(false);
            canvas.transform.Find("HungerBar").gameObject.SetActive(false);
            canvas.transform.Find("ArmorBar").gameObject.SetActive(false);
        }
        else
        {
            canvas.transform.Find("Crosshair").gameObject.SetActive(true);
            canvas.transform.Find("ItemBar").gameObject.SetActive(true);
            canvas.transform.Find("ItemSelect").gameObject.SetActive(true);
            canvas.transform.Find("HealthBar").gameObject.SetActive(true);
            canvas.transform.Find("HungerBar").gameObject.SetActive(true);
            canvas.transform.Find("ArmorBar").gameObject.SetActive(true);
        }
    }
}
