using UnityEngine;
[ExecuteAlways]
public class WaterAnimation : MonoBehaviour
{   
        public Material waterMaterial;
        public float speed = 0.05f;


        void Update()
        {
            float offsetX = Time.time * speed;
            float offsetY = Time.time * speed * 0.3f;
            waterMaterial.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
        }
    
}