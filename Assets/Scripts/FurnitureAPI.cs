using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class FurnitureAPI : MonoBehaviour
{
    public GameObject[] furniturePrefabs;
    private int currentIndex = 0;
    private List<FurnitureData> fetchedFurniture = new List<FurnitureData>();
    private List<GameObject> instantiatedFurniture = new List<GameObject>();

    public TextMeshProUGUI nextItemText;
    public TextMeshProUGUI messageText;
    public Button nextFurnitureButton;
    public Button previousFurnitureButton;
    public Button resetButton;
    public Button rotateXButton;
    public Button rotateYButton;
    public Button rotateZButton;
    public Button deleteButton;
    public Button saveButton;
    public Button loadButton;

    void Awake()
    {
        StartCoroutine(FetchFurnitureData());
        SetupUI();
    }

    void SetupUI()
    {
        if (nextFurnitureButton != null)
            nextFurnitureButton.onClick.AddListener(LoadNextFurniture);
        if (previousFurnitureButton != null)
            previousFurnitureButton.onClick.AddListener(LoadPreviousFurniture);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetScene);
        if (rotateXButton != null)
        {
            rotateXButton.onClick.AddListener(() => RotateSelectedFurniture(Vector3.right));
            rotateXButton.interactable = false;
        }
        if (rotateYButton != null)
        {
            rotateYButton.onClick.AddListener(() => RotateSelectedFurniture(Vector3.up));
            rotateYButton.interactable = false;
        }
        if (rotateZButton != null)
        {
            rotateZButton.onClick.AddListener(() => RotateSelectedFurniture(Vector3.forward));
            rotateZButton.interactable = false;
        }
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteSelectedObject);
            deleteButton.interactable = false;
        }
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveScene);
        if (loadButton != null)
            loadButton.onClick.AddListener(LoadScene);

        UpdateNextItemText();
        UpdateMessageText("Select a furniture item and tap to place it.");
    }

    [System.Serializable]
    public class FurnitureData
    {
        public string id;
        public string name;
        public string category;
        public string description;
        public Dictionary<string, float> dimensions;
        public float price;
        public string image_path;
    }

    [System.Serializable]
    public class APIResponse
    {
        public bool success;
        public int count;
        public List<FurnitureData> data;
    }

    public void LoadNextFurniture()
    {
        ARFurniturePlacer placer = GetComponent<ARFurniturePlacer>();
        if (placer != null)
        {
            LoadNextFurniture(placer);
            UpdateNextItemText();
        }
        else
        {
            Debug.LogError("ARFurniturePlacer component not found on " + gameObject.name + "!");
        }
    }

    public void LoadPreviousFurniture()
    {
        ARFurniturePlacer placer = GetComponent<ARFurniturePlacer>();
        if (placer != null)
        {
            LoadPreviousFurniture(placer);
            UpdateNextItemText();
        }
        else
        {
            Debug.LogError("ARFurniturePlacer component not found on " + gameObject.name + "!");
        }
    }

    public void LoadNextFurniture(ARFurniturePlacer placer)
    {
        if (fetchedFurniture.Count == 0)
        {
            Debug.LogWarning("No furniture data fetched yet. Please wait for API call to complete.");
            UpdateMessageText("Waiting for furniture data...");
            return;
        }

        if (furniturePrefabs.Length == 0)
        {
            Debug.LogWarning("No furniture prefabs assigned in the Inspector!");
            UpdateMessageText("No furniture prefabs available!");
            return;
        }

        currentIndex = (currentIndex + 1) % fetchedFurniture.Count;
        int furnitureIndex = currentIndex;
        int prefabIndex = currentIndex % furniturePrefabs.Length;

        FurnitureData currentItem = fetchedFurniture[furnitureIndex];
        GameObject selectedPrefab = furniturePrefabs[prefabIndex];

        if (placer != null && selectedPrefab != null)
        {
            placer.SetFurnitureToPlace(selectedPrefab);
            Debug.Log($"Selected furniture: {selectedPrefab.name} (API Item: {currentItem.name}, Category: {currentItem.category})");
            UpdateMessageText($"Tap to place {currentItem.name}!");
        }
    }

    public void LoadPreviousFurniture(ARFurniturePlacer placer)
    {
        if (fetchedFurniture.Count == 0)
        {
            Debug.LogWarning("No furniture data fetched yet. Please wait for API call to complete.");
            UpdateMessageText("Waiting for furniture data...");
            return;
        }

        if (furniturePrefabs.Length == 0)
        {
            Debug.LogWarning("No furniture prefabs assigned in the Inspector!");
            UpdateMessageText("No furniture prefabs available!");
            return;
        }

        currentIndex = (currentIndex - 1 + fetchedFurniture.Count) % fetchedFurniture.Count;
        int furnitureIndex = currentIndex;
        int prefabIndex = currentIndex % furniturePrefabs.Length;

        FurnitureData currentItem = fetchedFurniture[furnitureIndex];
        GameObject selectedPrefab = furniturePrefabs[prefabIndex];

        if (placer != null && selectedPrefab != null)
        {
            placer.SetFurnitureToPlace(selectedPrefab);
            Debug.Log($"Selected furniture: {selectedPrefab.name} (API Item: {currentItem.name}, Category: {currentItem.category})");
            UpdateMessageText($"Tap to place {currentItem.name}!");
        }
    }

    public void ResetScene()
    {
        foreach (GameObject furniture in instantiatedFurniture)
        {
            if (furniture != null)
            {
                Destroy(furniture);
            }
        }
        instantiatedFurniture.Clear();
        Debug.Log("Scene reset. All furniture removed.");
        UpdateNextItemText();
        UpdateMessageText("Scene reset. Select a new furniture item.");
        UpdateRotationButtons();
    }

    public void RotateSelectedFurniture(Vector3 axis)
    {
        if (instantiatedFurniture.Count > 0)
        {
            GameObject lastFurniture = instantiatedFurniture[instantiatedFurniture.Count - 1];
            if (lastFurniture != null)
            {
                lastFurniture.transform.Rotate(axis, 90f);
                Debug.Log($"Rotated last furniture ({lastFurniture.name}) around {axis}");
                UpdateMessageText($"Rotated {lastFurniture.name} around {axis} axis.");
            }
        }
        else
        {
            Debug.LogWarning("No furniture instantiated to rotate!");
            UpdateMessageText("Please place furniture to rotate!");
        }
    }

    public void DeleteSelectedObject()
    {
        ARFurniturePlacer placer = GetComponent<ARFurniturePlacer>();
        if (placer != null)
        {
            placer.DeleteSelectedObject();
            UpdateRotationButtons();
        }
    }

    private void UpdateNextItemText()
    {
        if (nextItemText != null && fetchedFurniture.Count > 0)
        {
            int nextIndex = currentIndex % fetchedFurniture.Count;
            string nextItemName = fetchedFurniture[nextIndex].name;
            nextItemText.text = $"Next: {nextItemName}";
        }
        else
        {
            nextItemText.text = "Next: N/A";
        }
    }

    // Single definition of UpdateMessageText
    public void UpdateMessageText(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            StartCoroutine(FadeMessage());
        }
    }

    private IEnumerator FadeMessage()
    {
        yield return new WaitForSeconds(3f);
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    private void UpdateRotationButtons()
    {
        bool hasFurniture = instantiatedFurniture.Count > 0;
        if (rotateXButton != null) rotateXButton.interactable = hasFurniture;
        if (rotateYButton != null) rotateYButton.interactable = hasFurniture;
        if (rotateZButton != null) rotateZButton.interactable = hasFurniture;
        if (deleteButton != null) deleteButton.interactable = hasFurniture;
    }

    public void AddInstantiatedFurniture(GameObject furniture)
    {
        instantiatedFurniture.Add(furniture);
        UpdateRotationButtons();
        UpdateMessageText($"Placed {furniture.name}. Tap to select and move.");
    }

    public void RemoveInstantiatedFurniture(GameObject furniture)
    {
        instantiatedFurniture.Remove(furniture);
        UpdateRotationButtons();
        UpdateMessageText($"Deleted {furniture.name}.");
    }

    public void OnObjectSelected(GameObject obj)
    {
        UpdateMessageText($"Selected {obj.name}. Tap to move or use buttons to rotate/delete.");
        if (deleteButton != null) deleteButton.interactable = true;
    }

    public void OnObjectMoved(GameObject obj)
    {
        UpdateMessageText($"Moved {obj.name} to new position.");
    }

    public void SaveScene()
    {
        ARFurniturePlacer placer = GetComponent<ARFurniturePlacer>();
        if (placer != null)
        {
            placer.SaveScene();
            UpdateMessageText("Scene saved!");
        }
    }

    public void LoadScene()
    {
        ARFurniturePlacer placer = GetComponent<ARFurniturePlacer>();
        if (placer != null)
        {
            placer.LoadScene();
            UpdateMessageText("Scene loaded!");
        }
    }

    IEnumerator FetchFurnitureData()
    {
        string url = "https://furniture-api.fly.dev/v1/products?category=chair";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Raw API Response: " + json);

                APIResponse response = JsonUtility.FromJson<APIResponse>(json);
                if (response != null && response.success && response.data != null)
                {
                    fetchedFurniture = response.data;
                    Debug.Log("Fetched " + fetchedFurniture.Count + " furniture items.");
                    UpdateNextItemText();
                }
                else
                {
                    Debug.LogError("Failed to parse API response or no data. Raw JSON: " + json);
                    UpdateMessageText("Failed to fetch furniture data!");
                }
            }
            else
            {
                Debug.LogError("API Error: " + request.error + " (Status Code: " + request.responseCode + ")");
                UpdateMessageText("API error! Please check your connection.");
            }
        }
    }
}