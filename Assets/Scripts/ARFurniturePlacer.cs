using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;


#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ARFurniturePlacer : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;
    public GameObject furniturePrefab;
    public Button screenshotButton;
    public Slider lightingSlider;

    private List<GameObject> placedObjects = new List<GameObject>();
    private GameObject furnitureToPlace;
    private GameObject selectedObject;
    private bool isReadyToPlace = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private FurnitureAPI furnitureAPI;

    void Awake()
    {
        furnitureAPI = GetComponent<FurnitureAPI>();
        if (furnitureAPI == null)
        {
            Debug.LogError("FurnitureAPI component not found on " + gameObject.name + "!");
        }

        if (raycastManager == null)
        {
            Debug.LogError("ARRaycastManager not found on " + gameObject.name + "!");
        }
    }

    void Start()
    {
        screenshotButton.onClick.AddListener(TakeScreenshot);
        lightingSlider.onValueChanged.AddListener(AdjustLighting);

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif
    }

    void Update()
    {
        HandleTouchInput();
        HandleObjectManipulation();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPosition = Input.GetTouch(0).position;
            Debug.Log($"Touch detected at: {touchPosition}");

            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (placedObjects.Contains(hitObject))
                {
                    selectedObject = hitObject;
                    furnitureAPI.OnObjectSelected(selectedObject);
                    Debug.Log($"Selected object: {selectedObject.name}");
                    return;
                }
            }

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                if (selectedObject != null)
                {
                    selectedObject.transform.position = hitPose.position;
                    selectedObject.transform.rotation = hitPose.rotation;
                    Debug.Log($"Moved {selectedObject.name} to position: {hitPose.position}");
                    furnitureAPI.OnObjectMoved(selectedObject);
                }
                else if (isReadyToPlace && furnitureToPlace != null)
                {
                    GameObject newFurniture = Instantiate(furnitureToPlace, hitPose.position, hitPose.rotation);
                    placedObjects.Add(newFurniture);
                    furnitureAPI.AddInstantiatedFurniture(newFurniture);
                    isReadyToPlace = false;
                    Debug.Log($"Placed furniture: {newFurniture.name} at position: {hitPose.position}");
                }
            }
            else
            {
                Debug.LogWarning("No AR plane detected at touch position.");
            }
        }
    }

    void HandleObjectManipulation()
    {
        if (placedObjects.Count == 0) return;

        GameObject currentObject = selectedObject ?? placedObjects[placedObjects.Count - 1];

        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            Vector3 scale = currentObject.transform.localScale;
            scale -= Vector3.one * deltaMagnitudeDiff * 0.01f;
            scale = Vector3.Max(scale, Vector3.one * 0.1f);
            currentObject.transform.localScale = scale;
        }

        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            float rotationSpeed = 5f;
            Vector2 delta = Input.GetTouch(0).deltaPosition;
            currentObject.transform.Rotate(0, -delta.x * rotationSpeed, 0);
        }
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureScreenshot());
    }

    private IEnumerator CaptureScreenshot()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
        {
            Debug.LogError("Failed to capture screenshot!");
            furnitureAPI.UpdateMessageText("Failed to capture screenshot!");
            yield break;
        }

        string fileName = "ARInteriorDesign_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

#if UNITY_ANDROID
        // Use AndroidJavaClass to access Android's Environment.DIRECTORY_PICTURES
        string directoryPath;
        try
        {
            using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
            using (AndroidJavaObject picturesDir = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", environment.GetStatic<string>("DIRECTORY_PICTURES")))
            {
                directoryPath = Path.Combine(picturesDir.Call<string>("getAbsolutePath"), "ARInteriorDesign");
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllBytes(filePath, screenshot.EncodeToPNG());

            // Notify the media scanner
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.MEDIA_SCANNER_SCAN_FILE"))
            using (AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath))
            using (AndroidJavaObject uri = new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("fromFile", file))
            {
                intent.Call<AndroidJavaObject>("setData", uri);
                activity.Call("sendBroadcast", intent);
            }

            Debug.Log($"Screenshot saved to: {filePath}");
            furnitureAPI.UpdateMessageText($"Screenshot saved to Pictures/ARInteriorDesign/{fileName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save screenshot on Android: {e.Message}");
            furnitureAPI.UpdateMessageText("Failed to save screenshot to gallery!");
        }
#else
        // Fallback for other platforms
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(filePath, screenshot.EncodeToPNG());
        Debug.Log($"Screenshot saved to: {filePath}");
        furnitureAPI.UpdateMessageText($"Screenshot saved to app data (not visible in gallery).");
#endif

        Destroy(screenshot);
    }

    public void AdjustLighting(float value)
    {
        RenderSettings.ambientIntensity = value;
    }

    public void SaveScene()
    {
        string json = JsonUtility.ToJson(new SceneData(placedObjects));
        PlayerPrefs.SetString("SavedScene", json);
        Debug.Log("Scene saved!");
    }

    public void LoadScene()
    {
        string json = PlayerPrefs.GetString("SavedScene", "");
        if (!string.IsNullOrEmpty(json))
        {
            SceneData data = JsonUtility.FromJson<SceneData>(json);
            foreach (var objData in data.objects)
            {
                GameObject obj = Instantiate(furniturePrefab, objData.position, Quaternion.Euler(objData.rotation));
                obj.transform.localScale = objData.scale;
                placedObjects.Add(obj);
                furnitureAPI.AddInstantiatedFurniture(obj);
            }
            Debug.Log("Scene loaded!");
        }
    }

    public void SetFurnitureToPlace(GameObject prefab)
    {
        furnitureToPlace = prefab;
        furniturePrefab = prefab;
        isReadyToPlace = true;
        selectedObject = null;
        Debug.Log($"Next furniture to place: {prefab.name}");
    }

    public void DeleteSelectedObject()
    {
        if (selectedObject != null)
        {
            placedObjects.Remove(selectedObject);
            furnitureAPI.RemoveInstantiatedFurniture(selectedObject);
            Destroy(selectedObject);
            Debug.Log($"Deleted object: {selectedObject.name}");
            selectedObject = null;
        }
        else
        {
            Debug.LogWarning("No object selected to delete!");
            furnitureAPI.UpdateMessageText("Please select an object to delete!");
        }
    }

    public Vector3 GetSpawnPosition()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            return hits[0].pose.position;
        }
        return Vector3.zero;
    }
}

[System.Serializable]
public class SceneData
{
    public List<ObjectData> objects = new List<ObjectData>();
    public SceneData(List<GameObject> placedObjects)
    {
        foreach (var obj in placedObjects) objects.Add(new ObjectData(obj.transform));
    }
}

[System.Serializable]
public class ObjectData
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public ObjectData(Transform transform)
    {
        position = transform.position;
        rotation = transform.eulerAngles;
        scale = transform.localScale;
    }
}