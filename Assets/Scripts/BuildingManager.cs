using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("PlayerSettings")]
    [SerializeField] private FPS_Controller playerController;

    [Header("Build Objects")]
    [SerializeField] private List<GameObject> floorObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> wallObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> furnitures = new List<GameObject>();

    [Header("Build Settings")]
    [SerializeField] private SelectedBuildType currentBuildType;
    [SerializeField] private LayerMask connectorLayer;

    [Header("Destroy Settings")]
    [SerializeField] private bool isDestroying = false;
    private Transform lastHitDestroyTransform;
    private List<Material> lastHitMaterial = new List<Material>();


    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterialValid;
    [SerializeField] private Material ghostMaterialInvalid;
    [SerializeField] private float connectorsOverlapRadius = 1;
    [SerializeField] private float maxGroundAngle = 45;
 
    [Header("Internal State")]
    [SerializeField]  private bool isBuilding = false;
    [SerializeField] private int currentBuildingIndex;
    private GameObject ghostBuildGameObject;
    private bool isGhostInvalidPosition = false;
    private Transform modelParent = null;

    [Header("UI")]
    [SerializeField] private GameObject buildingUI;
    [SerializeField] private TMP_Text destroyText;

    private void Update() {
        
        if(Input.GetKeyDown(KeyCode.Tab)){
            ToogleBuildingUI(!buildingUI.activeInHierarchy);
        }
        if(isBuilding && !isDestroying){
            GhostBuild();
            if(Input.GetMouseButtonDown(0))
                PlaceBuild();
        }else if(ghostBuildGameObject){
            Destroy(ghostBuildGameObject);
            ghostBuildGameObject = null;
        }
        if(isDestroying){
            GhostDestroy();
            if(Input.GetMouseButtonDown(0))
                DestroyBuild();
        }
    }

    

    private void GhostBuild()
    {
        GameObject currentBuild = GetCurrentBuild();
        CreateGhostPrefab(currentBuild);
        MoveGhostPRefabToRaycast();
        CheckBuildValidity();
    }

    private void CreateGhostPrefab(GameObject currentBuild)
    {
        if(ghostBuildGameObject == null){
            ghostBuildGameObject = Instantiate(currentBuild);

            modelParent = ghostBuildGameObject.transform.GetChild(0);

            GhostifyModel(modelParent, ghostMaterialValid);
            GhostifyModel(ghostBuildGameObject.transform);
        }
    }
    private void MoveGhostPRefabToRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
            ghostBuildGameObject.transform.position = hit.point;
    }
    private void CheckBuildValidity()
    {
        Collider[] colliders = Physics.OverlapSphere(ghostBuildGameObject.transform.position, connectorsOverlapRadius, connectorLayer);
        if(colliders.Length > 0 && !ghostBuildGameObject.CompareTag("Furnitures")){
            Debug.Log("First If");
            GhostConnectBuild(colliders);
        }else{
            GhostSeperateBuild();
            if(ghostBuildGameObject.CompareTag("Furnitures"))
               return;
                
                
            if(isGhostInvalidPosition){
                Collider[] overlapColiders = Physics.OverlapBox(ghostBuildGameObject.transform.position, new Vector3(2f, 2f, 2f), ghostBuildGameObject.transform.rotation);
                foreach (Collider overlapCollider in overlapColiders)
                {
                    if(overlapCollider.gameObject != ghostBuildGameObject && overlapCollider.transform.root.CompareTag("Buildables")){
                        GhostifyModel(modelParent, ghostMaterialInvalid);
                        isGhostInvalidPosition = false;
                        return;
                    }
                }
            }
        }
    }

    private void GhostConnectBuild(Collider[] colliders)
    {
        Connector bestConnector = null;
        foreach (Collider collider in colliders)
        {
            Connector connector = collider.GetComponent<Connector>();
            if(connector.canConnectTo){
                bestConnector = connector;
                break;
            }
        }
        bool floorCheck = currentBuildType == SelectedBuildType.floor && bestConnector.isConnectedToFloor;
        bool wallCheck = currentBuildType == SelectedBuildType.wall && bestConnector.isConnectedToWall;
        bool isFurniture = currentBuildType == SelectedBuildType.furniture;
        if(bestConnector == null || floorCheck || wallCheck){ 
            Debug.Log("asdasdasd");
            GhostifyModel(modelParent, ghostMaterialInvalid);
            isGhostInvalidPosition = false;
            return;
        }
        SnapGhostPrefabToConnector(bestConnector);
    }

    private void SnapGhostPrefabToConnector(Connector connector)
    {
        Debug.Log(ghostBuildGameObject.transform.childCount);
        if(ghostBuildGameObject.CompareTag("Furnitures"))
            return;
        Transform ghostConnector = FindSnapConnector(connector.transform, ghostBuildGameObject.transform.GetChild(1));
        ghostBuildGameObject.transform.position = connector.transform.position - (ghostConnector.position - ghostBuildGameObject.transform.position);
        if(currentBuildType == SelectedBuildType.wall){
            Quaternion newRotation = ghostBuildGameObject.transform.rotation;
            newRotation.eulerAngles = new Vector3(newRotation.eulerAngles.x, connector.transform.rotation.eulerAngles.y, newRotation.eulerAngles.z);
            ghostBuildGameObject.transform.rotation = newRotation;
        }

        GhostifyModel(modelParent, ghostMaterialValid);
        isGhostInvalidPosition = true;
    }

    

    private void GhostSeperateBuild()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit)){
            if(currentBuildType == SelectedBuildType.wall){
                GhostifyModel(modelParent, ghostMaterialInvalid);
                isGhostInvalidPosition = false;
                return;
            }
            if(Vector3.Angle(hit.normal, Vector3.up) < maxGroundAngle){
                GhostifyModel(modelParent, ghostMaterialValid);
                isGhostInvalidPosition = true;
            }else{
                GhostifyModel(modelParent, ghostMaterialValid);
                isGhostInvalidPosition = false;
            }
        }
    }

    

    private Transform FindSnapConnector(Transform snapConnector, Transform ghostConnectorParent)
    {
        ConnectorPosition oppositeConnectorTag = GetOppositePosition(snapConnector.GetComponent<Connector>());
        foreach (Connector connector in ghostConnectorParent.GetComponentsInChildren<Connector>())
        {
            if(connector.connectorPosition == oppositeConnectorTag)
                return connector.transform;
        }
        return null;
    }

    private ConnectorPosition GetOppositePosition(Connector connector)
    {
        ConnectorPosition position = connector.connectorPosition;
        if(currentBuildType == SelectedBuildType.wall && connector.connectorParentType == SelectedBuildType.floor)
            return ConnectorPosition.bottom;

        if(currentBuildType == SelectedBuildType.floor && connector.connectorParentType == SelectedBuildType.wall && connector.connectorPosition == ConnectorPosition.top){
            if(connector.transform.root.rotation.y == 0)
                return GetConnectorClosesToPlayer(true);
            else    
                return GetConnectorClosesToPlayer(false);
        }

        switch (position){
            case ConnectorPosition.left:
                return ConnectorPosition.right;
            case ConnectorPosition.right:
                return ConnectorPosition.left;
            case ConnectorPosition.bottom:
                return ConnectorPosition.top;
            case ConnectorPosition.top:
                return ConnectorPosition.bottom;
            default:
                return ConnectorPosition.bottom;
        }
    }

    private ConnectorPosition GetConnectorClosesToPlayer(bool topBottom)
    {
        Transform cameraTransform = Camera.main.transform;
        if(topBottom)
            return cameraTransform.position.z >= ghostBuildGameObject.transform.position.z ? ConnectorPosition.bottom : ConnectorPosition.top;
        else
            return cameraTransform.position.x >= ghostBuildGameObject.transform.position.x ? ConnectorPosition.left : ConnectorPosition.right;
    }
    private void GhostifyModel(Transform modelParent, Material ghostMaterial = null)
    {
        if(ghostMaterial != null){
            foreach (MeshRenderer meshRenderer in modelParent.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterial;
            }
        }else{
            foreach (Collider modelColliders in modelParent.GetComponentsInChildren<Collider>())
            {
                modelColliders.enabled = false;
            }
        }
        
    }
    private GameObject GetCurrentBuild()
    {
        switch (currentBuildType)
        {
            case SelectedBuildType.floor:
                return floorObjects[currentBuildingIndex];
            case SelectedBuildType.wall:
                return wallObjects[currentBuildingIndex];
            case SelectedBuildType.furniture:
                return furnitures[currentBuildingIndex];
        }
        return null;
    }
    private void PlaceBuild()
    {
        if(ghostBuildGameObject != null && isGhostInvalidPosition){
            GameObject newBuild = Instantiate(GetCurrentBuild(), ghostBuildGameObject.transform.position, ghostBuildGameObject.transform.rotation);

            Destroy(ghostBuildGameObject);
            ghostBuildGameObject = null;

            //isBuilding = false;

            foreach (Connector connector in newBuild.GetComponentsInChildren<Connector>())
            {
                connector.UpdateConnectors(true);
            }
        }
    }
    private void GhostDestroy()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit)){
            if(hit.transform.root.CompareTag("Buildables")){
                if(!lastHitDestroyTransform){
                    lastHitDestroyTransform = hit.transform.root;

                    lastHitMaterial.Clear();
                    foreach (MeshRenderer lastHitMeshRenderer in lastHitDestroyTransform.GetComponentsInChildren<MeshRenderer>())
                    {
                        lastHitMaterial.Add(lastHitMeshRenderer.material);
                    }
                    GhostifyModel(lastHitDestroyTransform.GetChild(0), ghostMaterialInvalid);
                }else if(hit.transform.root != lastHitDestroyTransform)
                    ResetLastHitDestroyTransform();
            }else if(hit.transform.root != lastHitDestroyTransform)
                ResetLastHitDestroyTransform();
        }
    }

    private void ResetLastHitDestroyTransform(){
        int counter = 0;
        if(lastHitDestroyTransform == null)
            return;
        foreach (MeshRenderer lastHitMeshRenderer in lastHitDestroyTransform.GetComponentsInChildren<MeshRenderer>())
        {
            lastHitMeshRenderer.material = lastHitMaterial[counter];
            counter++;
        }
        lastHitDestroyTransform = null;
    }
    
    private void DestroyBuild()
    {
        if(lastHitDestroyTransform){
            foreach (Connector connector in lastHitDestroyTransform.GetComponentsInChildren<Connector>())
            {
                connector.gameObject.SetActive(false);
                connector.UpdateConnectors(true);
            }
            Destroy(lastHitDestroyTransform.gameObject);

            DestroyBuildingToogle(true);
            lastHitDestroyTransform = null;
        }
    }
    private void ToogleBuildingUI(bool active)
    {
        isBuilding = false;
        buildingUI.SetActive(active);

        playerController.canMove = !active;

        Cursor.visible = active;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
    }
    public void DestroyBuildingToogle(bool fromScript = false){
        if(fromScript){
            isDestroying = false;
            destroyText.text = "Destroy Off";
            destroyText.color = Color.green;
        }else{
            isDestroying = !isDestroying;
            destroyText.text = isDestroying ? "Destroy On" : "Destroy Off";
            destroyText.color = isDestroying ? Color.red : Color.green;
            ToogleBuildingUI(false);
        }
    }
    public void ChangeBuildTypeButton(string SelectedBuildType){
        if(System.Enum.TryParse(SelectedBuildType, out SelectedBuildType result)){
            currentBuildType = result;
        }else{
            Debug.Log("Build Type Doesnt Exist");
        }
    }
    public void StartBuildingButton(int buildIndex){
        currentBuildingIndex = buildIndex;
        ToogleBuildingUI(false);
        isBuilding = true;
    }
}
[System.Serializable]
public enum SelectedBuildType{
    floor,
    wall,
    furniture,
}
