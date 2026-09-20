using System.Collections;
using UnityEngine;

public class CirculoExplosivo : MonoBehaviour
{
    [Header("Configuración Explosión")]
    public float tiempoHastaExplotar = 1.2f;
    public float duracionExplosion = 0.2f;
    public float dano = 2f;
    public GameObject colliderDano; // Objeto que contiene el Collider2D en modo Trigger

    [Header("Efectos Visuales")]
    private SpriteRenderer spriteRenderer;
    public Color colorInicial = Color.white;
    public Color colorFinal = Color.red;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = colorInicial;
        }

        if (colliderDano != null)
        {
            colliderDano.SetActive(false);
        }
    }

    private void Start()
    {
        StartCoroutine(RutinaExplotar());
    }

    private IEnumerator RutinaExplotar()
    {
        float t = 0f;

        while (t < tiempoHastaExplotar)
        {
            t += Time.deltaTime * Random.Range(0.9f, 1.1f);
            float progreso = t / tiempoHastaExplotar;

            yield return null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = colorFinal;
        }

        if (colliderDano != null)
        {
            colliderDano.SetActive(true);
        }

        yield return new WaitForSeconds(duracionExplosion);


        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (colliderDano != null && colliderDano.TryGetComponent(out CircleCollider2D col))
        {
            Gizmos.DrawWireSphere(colliderDano.transform.position, col.radius * colliderDano.transform.lossyScale.x);
        }
    }
}