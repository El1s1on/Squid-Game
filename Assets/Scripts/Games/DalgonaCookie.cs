using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DalgonaCookie : MonoBehaviour
{
    [SerializeField, Range(1, 100)] private int autoCompleteProgress;
    [SerializeField] private ParticleSystem cutVFX;
    [SerializeField] private LayerMask cookieMask;
    [SerializeField] private List<Renderer> outlineParts;
    [SerializeField] private List<GameObject> crackStages;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform pick;
    [SerializeField] private Material doneMaterial;
    [SerializeField] private float breakIntensity = 2.5f;

    private Camera playerCamera;
    private PlayerLook playerLook;
    private SkinnedMeshRenderer playerRenderer;
    private List<Renderer> remainingOutlineParts;
    private PlayerDalgonaData playerDalgonaData;
    public int currentCrackStage { get; private set; }
    private bool gameStarted = false;
    private bool readyForCrack = true;
    private float breakProgress;
    private float progress;
    private RaycastHit hit;

    public event System.Action<PlayerDalgonaData> OnBreak;
    public event System.Action<PlayerDalgonaData> OnComplete;

    public void Initialize(PlayerDalgonaData playerDalgonaData)
    {
        this.playerDalgonaData = playerDalgonaData;
        playerLook = playerDalgonaData.Player.GetComponent<PlayerLook>();
        playerCamera = playerDalgonaData.Player.GetComponentInChildren<Camera>();
        playerRenderer = playerDalgonaData.Player.GetComponentInChildren<SkinnedMeshRenderer>();
        remainingOutlineParts = new List<Renderer>(outlineParts);
        UpdateCrackStage();
    }

    public void StartMiniGame()
    {
        if(playerLook)
            playerLook.SetCameraUncontrolled(true);

        if (playerCamera && playerLook.IsCameraUncontrolled)
        {
            playerCamera.transform.DOMove(cameraTransform.position, 2.5f);
            playerCamera.transform.DORotate(cameraTransform.eulerAngles, 2.5f);
        }

        gameStarted = true;
    }

    private void Update()
    {
        UpdatePlayerRendererVisibility();
        HandlePlayerInput();
        HandlePick();
    }

    private void HandlePick()
    {
        Quaternion targetPickRotation;

        if (playerCamera && playerLook.IsCameraUncontrolled)
        {
            pick.position = hit.point;

            if(Input.GetMouseButton(0) && CanCut())
                targetPickRotation = Quaternion.Euler(45, 0, -30);
            else
                targetPickRotation = Quaternion.Euler(45, 0, -45);
        }
        else
        {
            targetPickRotation = Quaternion.Euler(45, 0, -45);
        }

        pick.rotation = Quaternion.Lerp(pick.rotation, targetPickRotation, 5f * Time.deltaTime);
        pick.gameObject.SetActive(playerCamera && playerLook.IsCameraUncontrolled);
    }

    private void UpdatePlayerRendererVisibility()
    {
        if (playerRenderer && playerCamera)
        {
            float distance = Vector3.Distance(playerCamera.transform.position, cameraTransform.position);
            playerRenderer.enabled = distance > 0.25f;
        }
    }

    private void HandlePlayerInput()
    {
        if (!playerCamera) return;

        var ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 1f, cookieMask))
        {
            if (!CanCut()) return;

            if (Input.GetMouseButton(0))
                ProcessCuttingInput(ray);
            else
                breakProgress = 0f;
        }
        else
        {
            breakProgress = 0f;
        }

        if (breakProgress > 1f && readyForCrack)
            StartCoroutine(AdvanceCrackStage());
    }

    private void ProcessCuttingInput(Ray ray)
    {
        if (hit.transform.CompareTag("CookieOutline"))
        {
            CutOutlinePart(hit.transform.gameObject);
        }
        else
        {
            var colliders = Physics.OverlapSphere(hit.point, 0.025f, cookieMask);
            if (!colliders.Any(obj => obj.CompareTag("CookieOutline")))
            {
                breakProgress += breakIntensity * Time.deltaTime;
            }
        }
    }

    public IEnumerator AdvanceCrackStage()
    {
        currentCrackStage++;
        readyForCrack = false;
        breakProgress = 0f;

        UpdateCrackStage();
        ShakeCamera(2f, 0.5f);

        if (currentCrackStage == crackStages.Count - 1)
        {
            yield return StartCoroutine(BreakCookie());
        }

        yield return new WaitForSeconds(1f);
        readyForCrack = true;
    }

    private void DisableAllOutlineParts()
    {
        foreach (var outlinePart in outlineParts)
        {
            outlinePart.gameObject.SetActive(false);
        }
    }

    private IEnumerator MoveCameraUpwards()
    {
        if (!playerCamera) yield break;

        playerCamera.transform.DOMoveY(playerCamera.transform.position.y + 2f, 3f);
        yield return new WaitForSeconds(3.5f);
    }

    private void ResetPlayerControl()
    {
        if(playerLook)
            playerLook.SetCameraUncontrolled(false);

        transform.parent = null;
    }

    private void UpdateCrackStage()
    {
        foreach (var crackStage in crackStages)
        {
            crackStage.SetActive(false);
        }
        crackStages[currentCrackStage].SetActive(true);
    }

    public bool CanCut()
    {
        return gameStarted &&
               readyForCrack &&
               currentCrackStage < crackStages.Count - 1 &&
               progress < autoCompleteProgress;
    }

    private void ShakeCamera(float shakeStrength, float shakeDuration)
    {
        if (!playerCamera) return;

        playerCamera.transform.DOPunchRotation(
            new Vector3(shakeStrength, shakeStrength, shakeStrength),
            shakeDuration)
            .OnComplete(() =>
            {
                playerCamera.transform.DORotate(cameraTransform.eulerAngles, 0.5f);
            });
    }

    private void CutOutlinePart(GameObject part)
    {
        var outlinePart = remainingOutlineParts
            .FirstOrDefault(obj => obj != null && obj.gameObject == part);

        if (outlinePart != null)
        {
            ParticleSystem cutVFX = Instantiate(this.cutVFX, pick.position + Vector3.up * .015f, Quaternion.Euler(-90f, 0f, 0f));
            cutVFX.Play();
            Destroy(cutVFX.gameObject, 3f);

            outlinePart.material = doneMaterial;
            remainingOutlineParts.Remove(outlinePart);
            UpdateCuttingProgress();
        }
    }

    private void UpdateCuttingProgress()
    {
        int cutsDone = outlineParts.Count - remainingOutlineParts.Count;
        progress = (float)cutsDone / outlineParts.Count * 100;
        
        if(progress >= autoCompleteProgress)
        {
            StartCoroutine(CompleteCookie());
        }
    }

    public IEnumerator CompleteCookie()
    {        
        var partsToProcess = remainingOutlineParts.ToList();

        foreach (var part in partsToProcess)
            CutOutlinePart(part.gameObject);
        Transform shape = crackStages[currentCrackStage].transform.Find("Shape");
        shape.transform.DOMoveY(shape.transform.position.y + 0.25f, 2.5f);
        FadeMaterial(shape.gameObject);

        yield return new WaitForSeconds(0.75f);
        DisableAllOutlineParts();
        yield return MoveCameraUpwards();

        ResetPlayerControl();
        OnComplete?.Invoke(playerDalgonaData);
    }

    public IEnumerator BreakCookie()
    {
        DisableAllOutlineParts();
        yield return MoveCameraUpwards();
        ResetPlayerControl();

        OnBreak?.Invoke(playerDalgonaData);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || playerCamera == null) return;

        Gizmos.color = Color.red;
        var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction);
        Gizmos.DrawWireSphere(hit.point, 0.01f);
    }

    private void FadeMaterial(GameObject fadeObject)
    {
        Material material = fadeObject.GetComponent<Renderer>().material;

        material.SetFloat("_Mode", 2);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;

        Color startColor = material.color;
        startColor.a = 1f;
        material.color = startColor;

        material.DOFade(0f, 1);
    }
}
