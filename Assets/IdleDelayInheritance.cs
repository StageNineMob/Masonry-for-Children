using UnityEngine;
using System.Collections;

public class IdleDelayInheritance : StateMachineBehaviour {
    private Animator parentAnimator = null;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("playIdle", false);

        if (parentAnimator == null)
        {
            parentAnimator = animator.gameObject.transform.parent.gameObject.GetComponent<Animator>();
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (parentAnimator.GetBool("playIdle"))
        {
            animator.SetBool("playIdle", true);
        }
    }


}
