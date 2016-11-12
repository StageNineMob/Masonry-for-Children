using UnityEngine;
using System.Collections;

public class IdleDelayScript : StateMachineBehaviour {
    private float exitTimer;
    private const float MIN_DELAY_TIME = 1f, MAX_DELAY_TIME = 5f;

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("playIdle", false);

        exitTimer = Time.time + Random.Range(MIN_DELAY_TIME, MAX_DELAY_TIME);
	}

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Time.time >= exitTimer)
        {
            animator.SetBool("playIdle", true);
        }
    }







}
