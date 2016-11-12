using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Random = UnityEngine.Random;

namespace StageNine
{
    public abstract class ActorAnimation
    {
        //enums
        //MAYBEDO: add an enum to identify this as an attack/death/idle/etc animation for database style loading.
        //subclasses

        //consts and static data
        public static ActorAnimation meleeAttackAnimation = new MeleeAttackAnimation();
        public static ActorAnimation rangedAttackAnimation = new RangedAttackAnimation();
        public static ActorAnimation rangedHitAnimation = new RangedHitAnimation();
        public static ActorAnimation artilleryAttackAnimation = new ArtilleryAttackAnimation();
        public static ActorAnimation artilleryHitAnimation = new ArtilleryHitAnimation();
        //public data

        //private data

        //public properties
        public virtual float estimatedDuration
        {
            get
            {
                return 0f; // placeholder for animations where this value won't be referenced
            }
        }

        //methods
        #region public methods

        public ActorAnimation()
        {

        }

        public abstract IEnumerator Play(AnimaticPopup animatic, GameObject actor, RandomSoundPackage startSound, RandomSoundPackage hitSound, Sprite weaponSprite);
        #endregion

        #region private methods

        #endregion
    }

    public class MeleeAttackAnimation : ActorAnimation
    {
        const float DRAW_ANIMATION_TIME = 0.3f;
        const float WEAPON_FULL_SIZE = 1f;
        const float WEAPON_SWING_TIME = 0.3f;
        const float WEAPON_DISTANCE = 0.3f;
        const float MOVE_SPEED = 8f;
        readonly float ESTIMATED_DURATION = ((AnimaticPopup.DEFENDER_POS + AnimaticPopup.GRID_X * AnimaticPopup.GRID_SIZE).x / MOVE_SPEED) + AnimaticPopup.MAXIMUM_ANIMATION_DELAY;

        //public properties
        public override float estimatedDuration
        {
            get
            {
                return ESTIMATED_DURATION;
            }
        }

        public override IEnumerator Play(AnimaticPopup animatic, GameObject actor, RandomSoundPackage startSound, RandomSoundPackage hitSound, Sprite weaponSprite)
        {
            var initialPosition = actor.transform.localPosition;
            var facing = Mathf.Sign(actor.transform.localScale.x);
            var velocity = Vector3.left * MOVE_SPEED * facing;//flips animation depending on facing
            bool swungWeapon = false;

            yield return new WaitForSeconds(Random.Range(0f, AnimaticPopup.MAXIMUM_ANIMATION_DELAY));

            if(startSound != null)
            {
                startSound.Play(actor.GetComponent<AudioSource>());
            }

            // TODO: Can we guarantee that weapons are drawn over their wielders?
            GameObject weaponExtra = animatic.RequestExtra();
            weaponExtra.transform.SetParent(actor.transform);
            weaponExtra.transform.localScale = Vector3.zero;
            weaponExtra.transform.localPosition = Vector3.up * WEAPON_DISTANCE;
            weaponExtra.transform.localRotation = Quaternion.identity;
            weaponExtra.GetComponent<Image>().sprite = weaponSprite;
            weaponExtra.GetComponent<Image>().color = Color.white;

            // TODO: research how safe this is
            animatic.StartCoroutine(DrawWeapon(weaponExtra));

            while (actor.transform.localPosition.x * facing > AnimaticPopup.SCREEN_CENTER && AnimaticPopup.animationRunning)
            {
                actor.transform.localPosition += velocity * Time.deltaTime;
                if (swungWeapon == false)
                {
                    if (actor.transform.localPosition.x * facing < AnimaticPopup.SCREEN_CENTER - velocity.x * facing * WEAPON_SWING_TIME)
                    {
                        swungWeapon = true;
                        animatic.StartCoroutine(SwingWeapon(weaponExtra, facing, hitSound));
                    }
                }
                yield return null;
            }

            while (actor.transform.localPosition.x * facing < initialPosition.x * facing && AnimaticPopup.animationRunning)
            {
                actor.transform.localPosition -= velocity * Time.deltaTime;
                yield return null;
            }
            actor.transform.localPosition = initialPosition;
        }

        private IEnumerator DrawWeapon(GameObject weapon)
        {
            float elapsedTime = 0f;
            while(elapsedTime < DRAW_ANIMATION_TIME)
            {
                weapon.transform.localScale = Vector3.one * elapsedTime * WEAPON_FULL_SIZE / DRAW_ANIMATION_TIME;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            weapon.transform.localScale = Vector3.one * WEAPON_FULL_SIZE;
        }

        private IEnumerator SwingWeapon(GameObject weapon, float facing, RandomSoundPackage hitSound)
        {
            float elapsedTime = 0f;
            while (elapsedTime < WEAPON_SWING_TIME)
            {
                weapon.transform.rotation = Quaternion.Euler(0f, 0f, 90f * elapsedTime / WEAPON_SWING_TIME);
                weapon.transform.localPosition = new Vector3(Mathf.Sin(Mathf.PI * -0.5f * elapsedTime / WEAPON_SWING_TIME) * WEAPON_DISTANCE, Mathf.Cos(Mathf.PI * 0.5f * elapsedTime / WEAPON_SWING_TIME) * WEAPON_DISTANCE);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            weapon.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            weapon.transform.localPosition = Vector3.left * WEAPON_DISTANCE;
            if (hitSound != null)
            {
                hitSound.Play(weapon.GetComponent<AudioSource>());
            }

            elapsedTime = DRAW_ANIMATION_TIME;
            while (elapsedTime > 0f)
            {
                weapon.transform.localScale = Vector3.one * elapsedTime * WEAPON_FULL_SIZE / DRAW_ANIMATION_TIME;
                elapsedTime -= Time.deltaTime;
                yield return null;
            }
            weapon.transform.localScale = Vector3.zero;
        }
    }

    public class RangedAttackAnimation : ActorAnimation
    {
        public const float X_VEL = 15f;
        public const float Y_INIT = 5f;
        public const float Y_ACCEL = -3f;
        readonly float ESTIMATED_DURATION = ((AnimaticPopup.DEFENDER_POS + AnimaticPopup.GRID_X * AnimaticPopup.GRID_SIZE).x / X_VEL) + AnimaticPopup.MAXIMUM_ANIMATION_DELAY;

        //public properties
        public override float estimatedDuration
        {
            get
            {
                return ESTIMATED_DURATION;
            }
        }

        public override IEnumerator Play(AnimaticPopup animatic, GameObject actor, RandomSoundPackage startSound, RandomSoundPackage hitSound, Sprite weaponSprite)
        {
            yield return new WaitForSeconds(Random.Range(0f, AnimaticPopup.MAXIMUM_ANIMATION_DELAY));

            var startPos = actor.transform.position;
            var offsetPos = Vector3.right * -.1f * actor.transform.localScale.x;//flips animation depending on facing

            if (startSound != null)
            {
                startSound.Play(actor.GetComponent<AudioSource>());
            }
            // TODO: Can we guarantee that weapons are drawn over their wielders?
            GameObject weaponExtra = animatic.RequestExtra();
            weaponExtra.transform.localScale = Vector3.one;
            weaponExtra.transform.localPosition = actor.transform.localPosition;
            weaponExtra.transform.localRotation = Quaternion.identity;
            weaponExtra.GetComponent<Image>().sprite = weaponSprite;
            weaponExtra.GetComponent<Image>().color = Color.white;

            // TODO: research how safe this is
            animatic.StartCoroutine(ShootWeapon(weaponExtra, animatic, hitSound));

            int count;
            for (count = 0; count < 6; count++)
            {
                switch (count % 4)
                {
                    case 0:
                        actor.transform.position += offsetPos;
                        break;
                    case 1:
                        actor.transform.position -= offsetPos;
                        break;
                    case 2:
                        actor.transform.position -= offsetPos;
                        break;
                    case 3:
                        actor.transform.position += offsetPos;
                        break;
                }
                yield return new WaitForSeconds(.05f);
            }
        }

        private IEnumerator ShootWeapon(GameObject weapon, AnimaticPopup animatic, RandomSoundPackage hitSound)
        {
            float yVel = Y_INIT;
            float sign = Mathf.Sign(weapon.transform.localPosition.x);
            while (weapon.transform.localPosition.x * sign > 0f)
            {
                var vel = new Vector3(X_VEL * -sign, yVel);
                yVel += Y_ACCEL * Time.deltaTime;
                var angle = Mathf.Atan2(vel.x, vel.y);
                weapon.transform.localPosition += vel * Time.deltaTime;
                weapon.transform.rotation = Quaternion.Euler(0, 0, -180 * angle / Mathf.PI);
                yield return null;
            }
            animatic.FindVictim(-sign, weapon.GetComponent<Image>().sprite, hitSound, rangedHitAnimation);
            weapon.SetActive(false);
        }
    }

    public class RangedHitAnimation : ActorAnimation
    {
        public override IEnumerator Play(AnimaticPopup animatic, GameObject actor, RandomSoundPackage startSound, RandomSoundPackage hitSound, Sprite weaponSprite)
        {
            // TODO: Can we guarantee that weapons are drawn over their wielders?
            GameObject weaponExtra = animatic.RequestExtra();
            weaponExtra.transform.localScale = Vector3.one;
            weaponExtra.transform.localPosition = actor.transform.localPosition;
            weaponExtra.transform.localRotation = Quaternion.identity;
            weaponExtra.GetComponent<Image>().sprite = weaponSprite;
            weaponExtra.GetComponent<Image>().color = Color.white;

            float sign = Mathf.Sign(actor.transform.localPosition.x);
            float remainingTime = (actor.transform.localPosition.x * sign / RangedAttackAnimation.X_VEL);
            Vector3 destination = actor.transform.localPosition;

            while (remainingTime > 0f)
            {
                var vel = new Vector3(RangedAttackAnimation.X_VEL * sign, -RangedAttackAnimation.Y_INIT - RangedAttackAnimation.Y_ACCEL * remainingTime);
                var angle = Mathf.Atan2(vel.x, vel.y);
                weaponExtra.transform.localPosition = destination - new Vector3(remainingTime * RangedAttackAnimation.X_VEL * sign, -(RangedAttackAnimation.Y_INIT + RangedAttackAnimation.Y_ACCEL * 0.5f * remainingTime) * remainingTime);
                weaponExtra.transform.rotation = Quaternion.Euler(0, 0, -180 * angle / Mathf.PI);
                remainingTime -= Time.deltaTime;
                yield return null;
            }
            weaponExtra.GetComponent<Image>().enabled = false;
            if (hitSound != null)
            {
                hitSound.Play(weaponExtra.GetComponent<AudioSource>());
            }
        }
    }

    public class ArtilleryAttackAnimation : ActorAnimation
    {
        public const float X_VEL = 10f;
        public const float Y_INIT = 5f;
        public const float Y_ACCEL = -3f;
        public const float SPIN_RATE = 540f;
        public const float CAR_SIZE = 2f;
        readonly float ESTIMATED_DURATION = ((AnimaticPopup.DEFENDER_POS + AnimaticPopup.GRID_X * AnimaticPopup.GRID_SIZE).x / X_VEL) + AnimaticPopup.MAXIMUM_ANIMATION_DELAY;

        //public properties
        public override float estimatedDuration
        {
            get
            {
                return ESTIMATED_DURATION;
            }
        }

        public override IEnumerator Play(AnimaticPopup animatic, GameObject actor, RandomSoundPackage startSound, RandomSoundPackage hitSound, Sprite weaponSprite)
        {
            yield return new WaitForSeconds(Random.Range(0f, AnimaticPopup.MAXIMUM_ANIMATION_DELAY));

            var startPos = actor.transform.position;
            var offsetPos = Vector3.right * -.1f * actor.transform.localScale.x;//flips animation depending on facing

            if (startSound != null)
            {
                startSound.Play(actor.GetComponent<AudioSource>());
            }
            // TODO: Can we guarantee that weapons are drawn over their wielders?
            GameObject weaponExtra = animatic.RequestExtra();
            weaponExtra.transform.localScale = Vector3.one * CAR_SIZE;
            weaponExtra.transform.localPosition = actor.transform.localPosition;
            weaponExtra.transform.localRotation = Quaternion.identity;
            weaponExtra.GetComponent<Image>().sprite = weaponSprite;
            weaponExtra.GetComponent<Image>().color = Color.white;

            // TODO: research how safe this is
            animatic.StartCoroutine(ShootWeapon(weaponExtra, animatic, hitSound));

            int count;
            for (count = 0; count < 6; count++)
            {
                switch (count % 4)
                {
                    case 0:
                        actor.transform.position += offsetPos;
                        break;
                    case 1:
                        actor.transform.position -= offsetPos;
                        break;
                    case 2:
                        actor.transform.position -= offsetPos;
                        break;
                    case 3:
                        actor.transform.position += offsetPos;
                        break;
                }
                yield return new WaitForSeconds(.05f);
            }
        }

        private IEnumerator ShootWeapon(GameObject weapon, AnimaticPopup animatic, RandomSoundPackage hitSound)
        {
            float yVel = Y_INIT;
            float sign = Mathf.Sign(weapon.transform.localPosition.x);
            float angle = 0f;
            while (weapon.transform.localPosition.x * sign > 0f)
            {
                var vel = new Vector3(X_VEL * -sign, yVel);
                yVel += Y_ACCEL * Time.deltaTime;
                angle += SPIN_RATE * sign * Time.deltaTime;
                weapon.transform.localPosition += vel * Time.deltaTime;
                weapon.transform.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
            animatic.FindVictim(-sign, weapon.GetComponent<Image>().sprite, hitSound, artilleryHitAnimation);
            weapon.SetActive(false);
        }
    }

    public class ArtilleryHitAnimation : ActorAnimation
    {
        public override IEnumerator Play(AnimaticPopup animatic, GameObject actor, RandomSoundPackage startSound, RandomSoundPackage hitSound, Sprite weaponSprite)
        {
            // TODO: Can we guarantee that weapons are drawn over their wielders?
            GameObject weaponExtra = animatic.RequestExtra();
            weaponExtra.transform.localScale = Vector3.one * ArtilleryAttackAnimation.CAR_SIZE;
            weaponExtra.transform.localPosition = actor.transform.localPosition;
            weaponExtra.transform.localRotation = Quaternion.identity;
            weaponExtra.GetComponent<Image>().sprite = weaponSprite;
            weaponExtra.GetComponent<Image>().color = Color.white;

            float sign = Mathf.Sign(actor.transform.localPosition.x);
            float remainingTime = (actor.transform.localPosition.x * sign / ArtilleryAttackAnimation.X_VEL);
            float angle = 0f;
            Vector3 destination = actor.transform.localPosition;

            while (remainingTime > 0f)
            {
                var vel = new Vector3(ArtilleryAttackAnimation.X_VEL * sign, -ArtilleryAttackAnimation.Y_INIT - ArtilleryAttackAnimation.Y_ACCEL * remainingTime);
                angle += ArtilleryAttackAnimation.SPIN_RATE * -sign * Time.deltaTime;
                weaponExtra.transform.localPosition = destination - new Vector3(remainingTime * ArtilleryAttackAnimation.X_VEL * sign, -(ArtilleryAttackAnimation.Y_INIT + ArtilleryAttackAnimation.Y_ACCEL * 0.5f * remainingTime) * remainingTime);
                weaponExtra.transform.rotation = Quaternion.Euler(0, 0, angle);
                remainingTime -= Time.deltaTime;
                yield return null;
            }
            weaponExtra.GetComponent<Image>().enabled = false;
            if (hitSound != null)
            {
                hitSound.Play(weaponExtra.GetComponent<AudioSource>());
            }
        }
    }
}
