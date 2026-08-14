using UnityEngine;

public class CameraHandler : MonoBehaviour {
    public Transform target;
    private BlockPlacer placer;

    private float rotationX;
    private float rotationY;
    private const float rotationSpeed = 3f;

    private float currentZoom;
    private const float zoomSpeed = 6f;
    private const float maxZoom = 13f;
    private const float minZoom = 25f;

    private int boardSize = 1;

    private Vector3 startTargetPosition;
    private Vector3 startPosition;
    private Vector3 startEuler;
    private Vector3 min;
    private Vector3 max;

    private void Start() {
        placer = FindObjectOfType<BlockPlacer>();
        transform.LookAt(target);

        // Aby se neupravila pozice kamery při prvním stiknutím RMB nebo kolečka myši, nastavíme aktuální hodnoty. 
        currentZoom = Vector3.Distance(transform.position, target.position);
        Vector3 angle = transform.localEulerAngles;
        rotationX = angle.x;
        rotationY = angle.y;

        startTargetPosition = target.transform.position;
        startPosition = transform.position;
        startEuler = transform.eulerAngles;
    }

    private void Update() {
        if (Input.GetKeyDown("r")) {
            SetCameraDistance(boardSize);
        }
    }

    private void LateUpdate() {
        if (!placer.InFocus()) return;
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetAxis("Mouse ScrollWheel") != 0) {
            ZoomCamera();
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(1)) {
            DragCameraVertically();
        }
        else if (Input.GetMouseButton(2)) {
            RotateCamera();
        }
        else if (!Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(1)) {
            DragCamera();
        }
    }

    // Pokud scrolujeme při stisknutí CTRL, kamera se oddálí nebo přiblíží k bodu na desce danou rychlostí.
    // Je nastaveno i omezení určující, jaká je max a min vzdálenost kamery od bodu, která záleží na velikosti desky.
    private void ZoomCamera() {
        //float zoomAmount = Input.GetAxis("Mouse Y") * zoomSpeed;
        float zoomAmount = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        float distance = Vector3.Distance(transform.position, target.position);
        currentZoom = Mathf.Clamp(distance - zoomAmount, maxZoom - (boardSize * 0.25f), minZoom + (boardSize - 12));

        Vector3 direction = transform.position - target.position;
        direction.Normalize();
        transform.position = target.position + direction * currentZoom;
    }

    // Pokud je stisknuto kolečko myši a pohybujeme se myší, kamera se bude otáčet kolem bodu na desce
    // v Xové a Yové ose určitou rychlostí, příčemž rotace na Y ose je omezena.
    private void RotateCamera() {
        placer.ResetBlockParameters();
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        rotationX -= mouseY;
        rotationY += mouseX;

        rotationX = Mathf.Clamp(rotationX, 25f, 60f);
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        transform.position = target.position - rotation * Vector3.forward * currentZoom;
        transform.LookAt(target);
    }

    // Zjistí posun myši pro posun kamery na X a Z ose
    private void DragCamera() {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        MoveCamera(new Vector3(-mouseX, 0, -mouseY));
    }

    // Zjistí posun myši pro posun kamery na Y ose
    private void DragCameraVertically() {
        float mouseY = Input.GetAxis("Mouse Y");
        MoveCamera(new Vector3(0, mouseY, 0));
    }

    // Posune kameru podle posunu myši správným směrem. Zároveň zajistí aby kamera nešla posunout mimo daný limit.
    // To je zařízeno tak, že kamera zároveň posouvá i bod na desce. Pokud se tento bod dotkne kraje desky, kamera
    // už nepůjde určitým směrem posunout dál.
    private void MoveCamera(Vector3 move) {
        move *= 0.25f;
        move = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * move;
        move = transform.InverseTransformDirection(move);

        Vector3 prevPosition = transform.position;
        target.parent = transform;

        transform.Translate(move, Space.Self);
        Vector3 targetPosition = target.position;
        targetPosition.x = Mathf.Min(Mathf.Max(targetPosition.x, min.x), max.x);
        targetPosition.y = Mathf.Max(targetPosition.y, 0);
        targetPosition.z = Mathf.Min(Mathf.Max(targetPosition.z, min.z), max.z);
        target.position = targetPosition;

        target.parent = null;
        KeepInBounds(targetPosition, prevPosition);
    }

    // Zařídí aby kamera nešla posunout dál, když se bod na desce, určený pozicí kamery,
    // nachází na kraji desky.
    private void KeepInBounds(Vector3 targetPosition, Vector3 prevPosition) {
        var transformPosition = transform.position;
        if (FloatEquals(targetPosition.x, max.x) && transform.position.x > prevPosition.x) {
            transformPosition.x = prevPosition.x;
        }
        if (FloatEquals(targetPosition.x, min.x) && transform.position.x < prevPosition.x) {
            transformPosition.x = prevPosition.x;
        }
        if (FloatEquals(targetPosition.z, max.z) && transform.position.z > prevPosition.z) {
            transformPosition.z = prevPosition.z;
        }
        if (FloatEquals(targetPosition.z, min.z) && transform.position.z < prevPosition.z) {
            transformPosition.z = prevPosition.z;
        }
        if (FloatEquals(targetPosition.y, 0) && transform.position.y < prevPosition.y) {
            transformPosition.y = prevPosition.y;
        }
        transform.position = transformPosition;
    }

    // Zjistí jestli se dvě float čísla rovnají.
    private bool FloatEquals(float a, float b) {
        const float tolerance = 0.001f;
        return Mathf.Abs(a - b) < tolerance;
    }

    // Resetuje otočení kamery na výchozí
    private void ResetAngle() {
        transform.eulerAngles = startEuler;
        Vector3 angle = transform.localEulerAngles;
        rotationX = angle.x;
        rotationY = angle.y;
    }

    // Nastaví vzdálenost kamery od desky podle velikosti této desky.
    public void SetCameraDistance(int size) {
        target.position = startTargetPosition;
        transform.position = startPosition;
        ResetAngle();
        
        boardSize = size;
        currentZoom = size * 1.2f;
        transform.position = target.position - transform.forward * currentZoom;
        transform.LookAt(target);
    }

    // Určí limit posunu kamery za pomocí krajních bodů desky.
    public void SetCameraBounds(GameObject board) {
        Bounds bounds = board.GetComponent<Collider>().bounds;
        min = bounds.min;
        max = bounds.max;
    }
}
