using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class AtaqueTiburon : MonoBehaviour
{
    public enum EstadoBoca { Reposo, Abierta, Bloqueada, Presa }

    [Header("Estado (Solo lectura)")]
    public EstadoBoca estadoActual = EstadoBoca.Reposo;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteMordisco;

    [Header("Daño")]
    [SerializeField] private int danoMordisco = 1;
    [SerializeField] private Collider2D colliderBoca;

    [Header("Agarre")]
    [SerializeField] private Transform puntoBoca;

    [Header("Bapuleo (Sacudida)")]
    [SerializeField] private int girosParaBapuleo = 4;
    [SerializeField] private float tiempoMaximoEntreGiros = 0.5f;
    [SerializeField] private int danoBapuleo = 1;

    [Header("Eventos de Bapuleo")]
    public UnityEvent onGolpeBapuleo;
    public UnityEvent onMuerteBapuleo;

    [Header("FX Gore / Sangre")]
    [SerializeField] private GameObject prefabEfectoCorte;
    [SerializeField] private GameObject prefabSangre;
    [SerializeField] private GameObject camara;

    private SpriteRenderer spriteRenderer;
    public static bool estaOcupado = false;

    private IAgarrable pezAgarrado = null;
    private GameObject pezAgarradoGO = null;
    private PlayerInput input;

    private MovimientoTiburon scriptMovimiento;
    private int contadorGiros = 0;
    private float ultimoTiempoGiro = 0f;
    private int ultimoLadoInput = 0;

    public bool tienePezAgarrado => pezAgarrado != null;
    private bool haMatado = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteNormal;
        colliderBoca.enabled = false;
        input = GetComponent<PlayerInput>();
        scriptMovimiento = GetComponent<MovimientoTiburon>();
    }

    public void OnAttack(InputValue value)
    {
        bool presionado = value.isPressed;

        if (presionado)
        {
            if (estadoActual == EstadoBoca.Reposo)
            {
                AbrirBoca();
            }
        }
        else
        {
            switch (estadoActual)
            {
                case EstadoBoca.Abierta:
                    CerrarBoca(EstadoBoca.Reposo);
                    break;
                case EstadoBoca.Presa:
                    SoltarPez();
                    CerrarBoca(EstadoBoca.Reposo);
                    break;
                case EstadoBoca.Bloqueada:
                    estadoActual = EstadoBoca.Reposo;
                    estaOcupado = false;
                    break;
            }
        }
    }

    private void AbrirBoca()
    {
        estadoActual = EstadoBoca.Abierta;
        estaOcupado = true;
        spriteRenderer.sprite = spriteMordisco;
        colliderBoca.enabled = true;
    }

    private void CerrarBoca(EstadoBoca nuevoEstado)
    {
        estadoActual = nuevoEstado;
        spriteRenderer.sprite = spriteNormal;
        colliderBoca.enabled = false;

        if (nuevoEstado == EstadoBoca.Reposo)
        {
            estaOcupado = false;
        }
    }

    private void Update()
    {
        if (estadoActual == EstadoBoca.Presa)
        {
            if (pezAgarradoGO == null || !pezAgarradoGO.activeInHierarchy)
            {
                LimpiarAgarre();
                CerrarBoca(EstadoBoca.Reposo); 
                return;
            }

            ProcesarBapuleo();
        }
    }

    private void ProcesarBapuleo()
    {
        if (scriptMovimiento == null) return;

        Vector2 inputMov = scriptMovimiento.getImput();
        int ladoActual = 0;

        if (inputMov.x > 0.1f) ladoActual = 1;
        else if (inputMov.x < -0.1f) ladoActual = -1;

        if (ladoActual != 0 && ladoActual != ultimoLadoInput)
        {
            if (ultimoLadoInput != 0)
            {
                float tiempoPasado = Time.time - ultimoTiempoGiro;
                if (tiempoPasado <= tiempoMaximoEntreGiros)
                {
                    contadorGiros++;
                }
                else
                {
                    contadorGiros = 1;
                }

                if (contadorGiros >= girosParaBapuleo)
                {
                    EjecutarGolpeBapuleo();
                }
            }
            else
            {
                contadorGiros = 1;
            }

            ultimoLadoInput = ladoActual;
            ultimoTiempoGiro = Time.time;
        }
    }

    private void EjecutarGolpeBapuleo()
    {
        if (pezAgarradoGO == null) return;

        VidaEnemigo enemigo = pezAgarradoGO.GetComponent<VidaEnemigo>() ?? pezAgarradoGO.GetComponentInParent<VidaEnemigo>();

        if (enemigo != null)
        {
            Vector3 posicionImpacto = pezAgarradoGO.transform.position;
            Vector2 dirRandom = Random.insideUnitCircle.normalized;

            GenerarEfectosImpacto(posicionImpacto, dirRandom, enemigo.transform);
            enemigo.RecibirDano(danoBapuleo, VidaEnemigo.TipoMuerte.Bapuleo);

            onGolpeBapuleo?.Invoke();

            if (pezAgarradoGO == null || !pezAgarradoGO.activeInHierarchy)
            {
                onMuerteBapuleo?.Invoke();
                LimpiarAgarre();
                CerrarBoca(EstadoBoca.Reposo);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        haMatado = false;
        if (estadoActual != EstadoBoca.Abierta) return;

        Vector2 direccionAtaque = (collision.transform.position - transform.position).normalized;
        if (direccionAtaque == Vector2.zero) direccionAtaque = transform.right;

        // 1. SI ES UN BOSS
        IVidaBoss boss = collision.GetComponent<IVidaBoss>() ?? collision.GetComponentInParent<IVidaBoss>();
        if (boss != null)
        {
            MonoBehaviour bossMB = boss as MonoBehaviour;
            Vector3 posicionBoss = bossMB != null ? bossMB.transform.position : collision.transform.position;
            Transform bossTransform = bossMB != null ? bossMB.transform : collision.transform;

            GenerarEfectosImpacto(posicionBoss, direccionAtaque, bossTransform);
            boss.RecibirDano(danoMordisco);

            Debug.Log("[AtaqueTiburon] ¡Mordisco certero al Boss!");

            CerrarBoca(EstadoBoca.Bloqueada);
            estaOcupado = false;
            return;
        }

        // 2. SI ES UN ENEMIGO COMÚN
        VidaEnemigo enemigo = collision.GetComponent<VidaEnemigo>() ?? collision.GetComponentInParent<VidaEnemigo>();
        if (enemigo != null)
        {
            IAgarrable agarrable = collision.GetComponent<IAgarrable>() ?? collision.GetComponentInParent<IAgarrable>();
            SaludTiburon saludJugador = GetComponent<SaludTiburon>() ?? GetComponentInParent<SaludTiburon>();

            Vector3 posImpacto = enemigo.transform.position;
            Transform enemigoTransform = enemigo.transform;

            GenerarEfectosImpacto(posImpacto, direccionAtaque, enemigoTransform);
            haMatado = enemigo.RecibirDano(danoMordisco, VidaEnemigo.TipoMuerte.Mordisco);

            if (haMatado && saludJugador != null)
            {
                saludJugador.Curar(1);
            }

            GameObject objetivoGO = (agarrable as MonoBehaviour)?.gameObject ?? collision.gameObject;

            if (agarrable != null && objetivoGO.activeInHierarchy)
            {
                AgarrarPez(agarrable, objetivoGO);
                estadoActual = EstadoBoca.Presa;
                colliderBoca.enabled = false;
            }
            else
            {
                CerrarBoca(EstadoBoca.Bloqueada);
            }
        }
        estaOcupado = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (estadoActual == EstadoBoca.Abierta)
            OnTriggerEnter2D(collision);
    }

    private void GenerarEfectosImpacto(Vector3 posicionImpacto, Vector2 direccionMordisco, Transform objetivo)
    {
        if (direccionMordisco.sqrMagnitude < 0.001f)
            direccionMordisco = Vector2.right;

        direccionMordisco.Normalize();
        posicionImpacto.z = -0.1f;

        if (prefabEfectoCorte != null)
        {
            float anguloCorte = Mathf.Atan2(direccionMordisco.y, direccionMordisco.x) * Mathf.Rad2Deg;
            Instantiate(prefabEfectoCorte, posicionImpacto, Quaternion.Euler(0, 0, anguloCorte));
        }

        if (prefabSangre != null)
        {
            float anguloSangre = Random.Range(0f, 360f) - 90f;
            GameObject sangreInstancia = Instantiate(prefabSangre, posicionImpacto, Quaternion.Euler(0, 0, anguloSangre));

            if (objetivo != null)
            {
                SeguirObjetivoFX seguidor = sangreInstancia.AddComponent<SeguirObjetivoFX>();
                seguidor.Inicializar(objetivo);
            }
        }

        camara.GetComponent<EfectosCamara>().AplicarImpacto();
    }

    private void AgarrarPez(IAgarrable agarrable, GameObject go)
    {
        pezAgarrado = agarrable;
        pezAgarradoGO = go;
        Transform parentTransform = puntoBoca != null ? puntoBoca : transform;
        pezAgarrado.EnAgarrar(parentTransform);
    }

    public void SoltarPez()
    {
        if (pezAgarrado != null)
        {
            pezAgarrado.EnSoltar();
            LimpiarAgarre();
        }
    }

    private void LimpiarAgarre()
    {
        pezAgarrado = null;
        pezAgarradoGO = null;
        contadorGiros = 0;
        ultimoLadoInput = 0;
        ultimoTiempoGiro = 0f;
    }

    public GameObject SoltarPezParaParry()
    {
        if (pezAgarrado == null) return null;

        GameObject go = pezAgarradoGO;
        pezAgarrado.EnSoltar();
        LimpiarAgarre();
        CerrarBoca(EstadoBoca.Reposo);
        return go;
    }

    public void addDamage(int nuevoDano)
    {
        danoMordisco += nuevoDano;
    }
}
// ============================================================================
// COMPONENTE AUXILIAR (Añadido al prefab de sangre)
// ============================================================================
public class SeguirObjetivoFX : MonoBehaviour
{
    private Transform objetivo;
    private Vector3 offset;
    private ParticleSystem sistemaParticulas;
    private bool detenido = false;

    public void Inicializar(Transform target)
    {
        if (target == null) return;
        objetivo = target;
        offset = transform.position - target.position;
        sistemaParticulas = GetComponent<ParticleSystem>();
    }

    private void LateUpdate()
    {
        if (detenido) return;

        // Si el objetivo sigue vivo y activo, la sangre le sigue
        if (objetivo != null && objetivo.gameObject.activeInHierarchy)
        {
            transform.position = objetivo.position + offset;
        }
        else
        {
            // En el momento en que muere o desaparece:
            detenido = true;

            if (sistemaParticulas != null)
            {
                // Corta la emisión de NUEVA sangre, pero mantiene las partículas existentes
                sistemaParticulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}