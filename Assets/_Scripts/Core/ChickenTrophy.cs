
using UnityEngine;
using UnityEngine.SceneManagement;


public class ChickenTrophy : MonoBehaviour
{
    [SerializeField, Range(0.01f, 10f)] private float radius = 1f;
    [SerializeField] private LayerMask checkLayer = default;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Collider collider in Physics.OverlapSphere(transform.position, radius, checkLayer))
        {
            if (collider.TryGetComponent(out Player player))
            {
                if (collider.transform.position.y < transform.position.y) { return; }
                SceneManager.LoadScene("WinScene");
            }
        }
    }


#if UNITY_EDITOR

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = new(1f, 0.7f, 0.7f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.white;
    }

#endif
}
