using UnityEngine;

public class ShipAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [Header("Audio samples")]
    [SerializeField] private AudioClip[] shootAudios;

    public void PlayAttack()
    {
        int index = Random.Range(0, shootAudios.Length);
        audioSource.PlayOneShot(shootAudios[index]);
        
    }
}
