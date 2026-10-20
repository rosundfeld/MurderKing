using UnityEngine;

//Purely cosmetic projectile: hits are resolved instantly on the server by CustomerManager.ApplyConeBlast
public class HotDogPellet : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color SausageColor = new Color(0.75f, 0.25f, 0.15f);

    private const float FLIGHT_TIME = 0.25f;

    private Vector3 start;
    private Vector3 end;
    private float timer;

    public static void Spawn(Vector3 origin, Vector3 direction, float distance)
    {
        GameObject pelletObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(pelletObject.GetComponent<Collider>());
        pelletObject.name = "HotDogPellet";
        pelletObject.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
        //Capsules are vertical, lay the sausage along the flight direction
        pelletObject.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor(BaseColorId, SausageColor);
        pelletObject.GetComponent<Renderer>().SetPropertyBlock(propertyBlock);

        HotDogPellet pellet = pelletObject.AddComponent<HotDogPellet>();
        pellet.start = origin;
        pellet.end = origin + direction * distance;
        pelletObject.transform.position = origin;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / FLIGHT_TIME;
        transform.position = Vector3.Lerp(start, end, t);
        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
