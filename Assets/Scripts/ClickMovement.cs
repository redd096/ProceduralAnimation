using UnityEngine;

public class ClickMovement : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float speed = 10f;

    private Camera cam;
    private Ray ray;
    private RaycastHit hit;
    private Vector3 targetPosition;

    void Start()
    {
        //get refs and default values
        cam = Camera.main;
        targetPosition = transform.position;
    }

    void Update()
    {
        //if click, rotate and set target position
        if (Input.GetMouseButtonDown(0))
        {
            ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, groundLayer))
            {
                transform.rotation = Quaternion.LookRotation(hit.point - transform.position, Vector3.up);
                targetPosition = hit.point;
            }
        }

        //teleport if really near
        if ((targetPosition - transform.position).sqrMagnitude < 0.02f)
        {
            transform.position = targetPosition;
        }
        //else move to target position
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }
}
