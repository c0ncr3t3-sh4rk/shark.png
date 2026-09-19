using UnityEngine;

public class EfectosCamara : MonoBehaviour
{
    public static EfectosCamara Instance;

    private Camera cam;

    [Header("Color Fondo Base (Azul Marino Oscilante)")]
    public Color colorAzulBase = new Color(0.02f, 0.08f, 0.2f);
    public Color colorAzulBrillante = new Color(0.05f, 0.15f, 0.35f);
    public float velocidadOscilacion = 0.8f;

    [Header("Efecto 1: Flashazo de Impacto (Rápido)")]
    public Color colorFlashRojo = new Color(0.9f, 0.1f, 0.1f);
    [Tooltip("Cuánto sube el flash con cada hit")]
    public float incrementoFlash = 0.5f;
    [Tooltip("Velocidad de apagado del flash (alta = muy rápido)")]
    public float velocidadApagadoFlash = 8f;

    [Header("Efecto 2: Marea / Sangre en el Agua (Lento)")]
    public Color colorSangreAgua = new Color(0.5f, 0.02f, 0.05f);
    [Tooltip("Cuánto tiñe la sangre acumulada por cada impacto")]
    public float incrementoSangre = 0.05f;
    [Tooltip("Velocidad a la que la corriente se lleva la sangre (baja = dura mucho)")]
    public float velocidadCorriente = 0.08f;
    public float tParaBorrar = 5f;

    [Header("Prefabs / Sistemas de Partículas de Sangre")]
    [SerializeField] private GameObject prefabSangrePeque;
    [SerializeField] private GameObject prefabSangreNormie;
    [SerializeField] private GameObject prefabSangreGrande;

    // Referencias a los componentes ParticleSystem
    private ParticleSystem psPeque;
    private ParticleSystem psNormie;
    private ParticleSystem psGrande;

    private float intensidadFlash = 0f;
    public float intensidadSangre = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // Obtenemos los componentes ParticleSystem de los objetos asignados
        if (prefabSangrePeque != null) psPeque = prefabSangrePeque.GetComponentInChildren<ParticleSystem>();
        if (prefabSangreNormie != null) psNormie = prefabSangreNormie.GetComponentInChildren<ParticleSystem>();
        if (prefabSangreGrande != null) psGrande = prefabSangreGrande.GetComponentInChildren<ParticleSystem>();

        // Ningun nivel debe emitir hasta alcanzar su umbral de intensidad.
        AjustarEmision(psPeque, false);
        AjustarEmision(psNormie, false);
        AjustarEmision(psGrande, false);
    }

    private void Update()
    {
        if (cam == null) return;

        float t = (Mathf.Sin(Time.time * velocidadOscilacion) + 1f) / 2f;
        Color colorAzulActual = Color.Lerp(colorAzulBase, colorAzulBrillante, t);

        // Determinamos qué nivel debe emitir según la intensidad
        bool esGrande = intensidadSangre >= 0.75f;
        bool esNormie = intensidadSangre >= 0.5f && !esGrande;
        bool esPeque = intensidadSangre >= 0.25f && !esNormie && !esGrande;

        AjustarEmision(psGrande, esGrande);
        AjustarEmision(psNormie, esNormie);
        AjustarEmision(psPeque, esPeque);

        if (intensidadFlash > 0f)
        {
            intensidadFlash -= Time.deltaTime * velocidadApagadoFlash;
            intensidadFlash = Mathf.Max(0f, intensidadFlash);
        }

        if (intensidadSangre > 0f && tParaBorrar <= 0f)
        {
            intensidadSangre -= Time.deltaTime * velocidadCorriente;
            intensidadSangre = Mathf.Max(0f, intensidadSangre);
        } 
        else if (tParaBorrar > 0f)
        {
            tParaBorrar -= Time.deltaTime;
            tParaBorrar = Mathf.Max(0f, tParaBorrar);
        }

        Color colorFondoConSangre = Color.Lerp(colorAzulActual, colorSangreAgua, intensidadSangre);
        cam.backgroundColor = Color.Lerp(colorFondoConSangre, colorFlashRojo, intensidadFlash);
    }

    /// <summary>
    /// Controla la emisión de un ParticleSystem sin apagar el GameObject.
    /// </summary>
    private void AjustarEmision(ParticleSystem ps, bool debeEmitir)
    {
        if (ps == null) return;

        // Aseguramos que el GameObject permanezca activo para renderizar las partículas vivas
        if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);

        var emission = ps.emission;

        if (emission.enabled != debeEmitir)
        {
            emission.enabled = debeEmitir;

            if (debeEmitir && !ps.isPlaying)
            {
                ps.Play();
            }
            else if (!debeEmitir && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    public void AplicarImpacto(float multFlash = 1f, float multSangre = 0.1f)
    {
        // Disparar flash rápido
        intensidadFlash += incrementoFlash * multFlash;
        intensidadFlash = Mathf.Clamp01(intensidadFlash);

        // Acumular sangre en la corriente
        intensidadSangre += incrementoSangre * multSangre;
        intensidadSangre = Mathf.Clamp01(intensidadSangre);

        tParaBorrar = 5f;
    }

    public void AplicarMuerte()
    {
        AplicarImpacto(1.5f, 2.5f);
    }
}