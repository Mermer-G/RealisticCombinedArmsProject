using UnityEditor;
using UnityEngine;

public class ThrottleArm : MonoBehaviour
{
    [SerializeField] AnimationCurve rotationCurve;
    [SerializeField] FlightControlSystem flightControlSystem;

    Animator animator;
    bool power;
    float localInput;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleAnimation();
    }

    private void OnEnable()
    {
        flightControlSystem.throttleInput += GetInput;
    }

    private void OnDisable()
    {
        flightControlSystem.throttleInput -= GetInput;
    }

    void GetInput(float input)
    {
        localInput = input;
    }
    float a;
    bool animationPlaying = false;
    void HandleAnimation()
    {
        // Input 0.1 olduğunda animasyonu başlat
        if (localInput == 0.1f && !animationPlaying)
        {
            animator.enabled = true; // animator devreye giriyor
            animator.Play("ThrottleToIdle", 0, 0);
            animationPlaying = true;
            power = true;
        }

        // Animasyon bitince animator'ı kapat
        if (animationPlaying)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("ThrottleToIdle") && stateInfo.normalizedTime >= 1f)
            {
                animator.enabled = false;
                animationPlaying = false;
            }
        }

        // Güç aktifse objeyi döndür
        if (power)
        {
            a = Mathf.Lerp(a, localInput, 0.05f);
            var rot = rotationCurve.Evaluate(a);
            transform.localRotation = Quaternion.Euler(rot, 0, 0);
        }

        //  Input sıfırsa ➔ Gücü ve animatorı sıfırla
        if (localInput == 0)
        {
            power = false;

            // Objeyi sıfıra (nötr throttle pozisyonuna) döndür
            transform.rotation = Quaternion.Euler(0, 0, 0);

            // Animator’ı sıfırla ve Idle pozisyonuna getir
            animator.enabled = true;
            animator.Play("Idle", 0, 0);
        }
    }
}
