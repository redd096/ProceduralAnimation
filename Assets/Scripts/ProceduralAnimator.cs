using System.Collections;
using UnityEngine;

public class ProceduralAnimator : MonoBehaviour
{
    [Tooltip("Targets to move")][SerializeField] private Transform[] limbTargets;
    [Tooltip("When foot reach this distance, do a step")][SerializeField] private float stepSize = 2;

    [Header("Step Animation")]
    [Tooltip("Move the foot also in height when do a step")][SerializeField] private float stepHeight = 0.5f;
    [Tooltip("Calculate Step animation duration in frames or seconds?")][SerializeField] private bool doAnimationInFrames = true;
    [Tooltip("Step animation duration in frames")][SerializeField] private float stepDurationInFrames = 2f;
    [Tooltip("Step animation duration in seconds")][SerializeField] private float stepDurationInSeconds = 0.03f;

    [Header("Raycast to find ground")]
    [SerializeField] private LayerMask groundLayerMask = default;
    [Tooltip("Raycast height. Foot position + (up * range)")][SerializeField] private float raycastRange = 2;
    [Tooltip("Move up feet, if setting target on ground put feet under ground")][SerializeField] private float feetVerticalOffset = 0;

    //limb for every target
    private int nLimbs;        //targets.Length
    private ProceduralLimb[] limbs;

    //body velocity
    private Vector3 lastBodyPosition;
    private Vector3 velocity;

    //to avoid to continue move back to rest position, even when every leg is already back
    private bool allLimbsResting;

    private void Start()
    {
        //set default vars
        nLimbs = limbTargets.Length;
        lastBodyPosition = transform.position;
        allLimbsResting = true;

        //foreach target, create a Procedural Limb
        limbs = new ProceduralLimb[nLimbs];
        Transform t;
        for (int i = 0; i < nLimbs; ++i)
        {
            t = limbTargets[i];
            limbs[i] = new ProceduralLimb()
            {
                IKTarget = t,
                defaultPosition = t.localPosition,
                lastPosition = t.position,
                moving = false,
            };
        }
    }

    private void FixedUpdate()
    {
        //calculate velocity (current position - last position)
        velocity = transform.position - lastBodyPosition;

        //move or back to rest position
        if (velocity.magnitude > Mathf.Epsilon)
            HandleMovement();
        else
            BackToRestPosition();
    }

    #region private API

    private void HandleMovement()
    {
        lastBodyPosition = transform.position;

        //"desired" position = default position but in world space
        Vector3[] desiredPositions = new Vector3[nLimbs];
        float greatestDistance = stepSize;
        int limbToMove = -1;

        //find if there is a leg to move (exceed step size). Find the one with greatest movement to move only that
        for (int i = 0; i < nLimbs; ++i)
        {
            if (limbs[i].moving) continue; //limb already moving: can't move again!

            //"desired" position + velocity (to know where want to move) - last position (to know how long until reach it)
            desiredPositions[i] = transform.TransformPoint(limbs[i].defaultPosition);
            float dist = (desiredPositions[i] + velocity - limbs[i].lastPosition).magnitude;
            if (dist > greatestDistance)
            {
                greatestDistance = dist;
                limbToMove = i;
            }
        }

        //keep non moving limbs in place
        //(This will make for a more realistic walk animation since, in real life, most creatures have to keep one point of contact with the ground as they move)
        //of course, this should be removed if you want the creature to jump with all its limbs in between positions
        for (int i = 0; i < nLimbs; ++i)
            if (i != limbToMove)
                limbs[i].IKTarget.position = limbs[i].lastPosition;

        //move the selected leg to its "desired" position
        if (limbToMove != -1)
        {
            Vector3 targetOffset = desiredPositions[limbToMove] - limbs[limbToMove].IKTarget.position;
            Vector3 targetPoint = desiredPositions[limbToMove] + velocity.magnitude * targetOffset;
            targetPoint = RaycastToGround(targetPoint, transform.up);  //raycast to check ground height
            targetPoint += transform.up * feetVerticalOffset;

            //set not resting
            allLimbsResting = false;

            //start step animation
            StartCoroutine(StepAnimationCoroutine(limbToMove, targetPoint));
        }
    }

    private void BackToRestPosition()
    {
        //if every leg is already resting, do nothing
        if (allLimbsResting)
            return;

        Vector3 targetPoint; 
        float dist;

        for (int i = 0; i < nLimbs; ++i)
        {
            if (limbs[i].moving) continue; //limb already moving: can't move again!

            //"desired" position = default position but in world space
            targetPoint = transform.TransformPoint(limbs[i].defaultPosition);

            targetPoint = RaycastToGround(targetPoint, transform.up);  //raycast to check ground height
            targetPoint += transform.up * feetVerticalOffset;

            //if the leg is distant from "desired" position, move to it
            dist = (targetPoint - limbs[i].lastPosition).magnitude;
            if (dist > 0.005f)
            {
                //start step animation
                StartCoroutine(StepAnimationCoroutine(i, targetPoint));
                return;
            }
        }

        //set resting
        allLimbsResting = true;
    }

    private Vector3 RaycastToGround(Vector3 pos, Vector3 up)
    {
        Vector3 point = pos;

        Ray ray = new Ray(pos + raycastRange * up, -up);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f * raycastRange, groundLayerMask))
            point = hit.point;

        return point;
    }

    private IEnumerator StepAnimationCoroutine(int limbIndex, Vector3 targetPosition)
    {
        //set is moving
        limbs[limbIndex].moving = true;

        Vector3 startPosition = limbs[limbIndex].lastPosition;
        float delta = 0;

        //do animation in frames
        if (doAnimationInFrames)
        {
            for (int i = 1; i <= stepDurationInFrames; ++i)
            {
                delta = i / (stepDurationInFrames + 1f);
                limbs[limbIndex].IKTarget.position = Vector3.Lerp(startPosition, targetPosition, delta)
                    + transform.up * Mathf.Sin(delta * Mathf.PI) * stepHeight;  //move up feet during animation

                //do always in fixed update
                yield return new WaitForFixedUpdate();
            }
        }
        //else do animation in sconds
        else
        {
            while (delta < 1)
            {
                delta += Time.deltaTime / stepDurationInSeconds;
                limbs[limbIndex].IKTarget.position = Vector3.Lerp(startPosition, targetPosition, delta)
                    + transform.up * Mathf.Sin(delta * Mathf.PI) * stepHeight;  //move up feet during animation

                //do always in fixed update
                yield return new WaitForFixedUpdate();
            }
        }

        //set final position
        limbs[limbIndex].IKTarget.position = targetPosition;
        limbs[limbIndex].lastPosition = targetPosition;

        //set is no more moving
        limbs[limbIndex].moving = false;
    }

    #endregion

    class ProceduralLimb
    {
        /// <summary>
        /// The reference to the transform of the IK target associated with this limb
        /// </summary>
        public Transform IKTarget;
        /// <summary>
        /// The rest position of the IK target (i.e. the "desired" position, considering the overall offset of the body). 
        /// So "desired" position = default position but in world space
        /// </summary>
        public Vector3 defaultPosition;
        /// <summary>
        /// The position of the IK target at the last frame (this will be useful for computing the direction towards the "desired" position)
        /// </summary>
        public Vector3 lastPosition;
        /// <summary>
        /// If the limb is already moving (doing step animation)
        /// </summary>
        public bool moving;
    }
}