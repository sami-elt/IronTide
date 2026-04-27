using System.Collections;
using UnityEngine;

public class WeaponProjectiles : MonoBehaviour 
{

    public float speed = 15f;

    public void Shoot(Transform target)
    {
        StartCoroutine(FlyToTarget(target));
    }

    private IEnumerator FlyToTarget(Transform target)
    {
        //Röra sig mot target
        while (target != null && Vector3.Distance(transform.position, target.position) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            yield return null;
        }

        //När det träffar blixtra rött
        if (target != null)
        {
            Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                Color originalColor = targetRenderer.material.color;
                targetRenderer.material.color = Color.red;

                //Gömma skottet
                GetComponent<Renderer>().enabled = false; 

                yield return new WaitForSeconds(0.2f);

                targetRenderer.material.color = originalColor; 
            }
        }

        //Tar bort kulan
        Destroy(gameObject);
    }
}
