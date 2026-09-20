using System.Collections;
using System;
using UnityEngine;

public enum EstadoMarrajo
{
    FueraDeCamara,
    Sierra,
    EmbestidaExplosivo,
    EmbestidaLarga,
    Frenazo,
    CorteV,
    Cuadricula
}

public class BossMako : MonoBehaviour, IVidaBoss, IBoss
{
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual;

    [Header("Embestida Larga")]
    public GameObject hitboxBoca;
    public float velocidadEmbestidaLarga = 25f;
    public float danoEmbestidaLarga = 2f;
    private int nEmbestidas = 0;

    [Header("Embestida Rápida (Explosiva / Directa)")]
    public float velocidadEmbestidaExplosiva = 50f;
    public float danoEmbestidaExplosiva = 3f;

    [Header("Sierra")]
    public GameObject hitboxSierra;
    public float tiempoSierra = 2f;
    public float velocidadRotacionSierra = 3240f;
    
    [Header("Ataque Sierra - Fase 2+")]
    public GameObject prefabCirculoExplosivo;
    public int cantidadCirculosSierra = 6;

    [Header("Salida de Pantalla")]
    public float velocidadSalida = 30f;
    public LineRenderer lineaTelegrafiado;

    [Header("General")]
    public EstadoMarrajo estadoActual;
    public int faseActual = 1;
    public float tiempoFrenazo = 1f;

    [Header("Referencias")]
    public Transform jugador;
    private Rigidbody2D rb;
    private PuntoEmbestida[] puntosDeEntrada;
    private BossUtils bossUtils;
    private Coroutine bucleIA;
    private bool muerteNotificada;
    public GameObject colisionPared;
    public MiniMako[] Hijos = new MiniMako[2];

    public event Action OnBossMuerto;

    private bool enStun = false;
    private bool haChocadoPared = false;
    private SpriteRenderer spriteRenderer;

    private EstadoMarrajo estadoAnterior = EstadoMarrajo.FueraDeCamara;
    private EstadoMarrajo estadoSiguiente;
    private int nMismoAtaque = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (colisionPared != null) colisionPared.SetActive(false);
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        vidaActual = vidaMaxima;

        if (hitboxSierra != null) hitboxSierra.SetActive(false);
        if (hitboxBoca != null) hitboxBoca.SetActive(false);

        bossUtils = GetComponentInParent<BossUtils>();
        if (bossUtils != null)
        {
            puntosDeEntrada = bossUtils.GetPuntosEmbestida();

            Hijos = bossUtils.GetComponentsInChildren<MiniMako>();
        }
    }

    private void Start()
    {
        if (bossUtils == null)
        {
            bossUtils = GetComponentInParent<BossUtils>();
            if (bossUtils != null)
                puntosDeEntrada = bossUtils.GetPuntosEmbestida();
        }
    }

    public void IniciarCombate()
    {
        if (bucleIA == null && vidaActual > 0)
            bucleIA = StartCoroutine(BucleIA());
    }

    private IEnumerator BucleIA()
    {
        while (vidaActual > 0)
        {
            float rand = UnityEngine.Random.value;
            float distancia = Vector2.Distance(transform.position, jugador.position);

            if (faseActual == 1) // FASE UNO <><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
            {
                // --- ATAQUE SIERRA (Cerca del jugador) ---
                if (distancia < 4f && rand < 0.6f)
                {
                    nMismoAtaque = 0; // Reiniciamos el contador si hace sierra en Fase 1
                    yield return StartCoroutine(AtaqueSierra());
                }
                // --- EMBESTIDA EXPLOSIVA ---
                else if (rand < 0.4f)
                {
                    if (estadoActual == EstadoMarrajo.EmbestidaExplosivo)
                        nMismoAtaque++;
                    else 
                        nMismoAtaque = 1;

                    // Máximo se repite 1 vez (2 ejecuciones seguidas)
                    if (nMismoAtaque > 1)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                }
                // --- EMBESTIDA LARGA ---
                else
                {
                    if (estadoActual == EstadoMarrajo.EmbestidaLarga)
                        nMismoAtaque++;
                    else 
                        nMismoAtaque = 1;

                    // Máximo se repite 1 vez (2 ejecuciones seguidas)
                    if (nMismoAtaque > 1)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                }
            }
            else if (faseActual == 2) // FASE DOS <><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
            {
                // --- ATAQUE SIERRA ---
                if (rand < 0.3f)
                {
                    if (estadoActual == EstadoMarrajo.Sierra)
                        nMismoAtaque++;
                    else
                        nMismoAtaque = 1;

                    // NUNCA dos sierras seguidas tras Fase 1
                    if (nMismoAtaque > 0 && estadoActual == EstadoMarrajo.Sierra)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueSierra());
                    }
                }
                // --- EMBESTIDA EXPLOSIVA ---
                else if (rand < 0.6f)
                {
                    if (estadoActual == EstadoMarrajo.EmbestidaExplosivo)
                        nMismoAtaque++;
                    else 
                        nMismoAtaque = 1;

                    // Máximo se repite 1 vez
                    if (nMismoAtaque > 1)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                }
                // --- EMBESTIDA LARGA ---
                else
                {
                    if (estadoActual == EstadoMarrajo.EmbestidaLarga)
                        nMismoAtaque++;
                    else 
                        nMismoAtaque = 1;

                    // Máximo se repite 1 vez
                    if (nMismoAtaque > 1)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                }
            }
            else if (faseActual == 3) // FASE TRES <><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
            {
                // --- ATAQUE SIERRA ---
                if (rand < 0.3f)
                {
                    if (estadoActual == EstadoMarrajo.Sierra)
                        nMismoAtaque++;
                    else
                        nMismoAtaque = 1;

                    // NUNCA dos sierras seguidas tras Fase 1
                    if (nMismoAtaque > 0 && estadoActual == EstadoMarrajo.Sierra)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueSierra());
                    }
                }
                // --- EMBESTIDA EXPLOSIVA ---
                else if (rand < 0.6f)
                {
                    if (estadoActual == EstadoMarrajo.EmbestidaExplosivo)
                        nMismoAtaque++;
                    else 
                        nMismoAtaque = 1;

                    // Máximo se repite 1 vez
                    if (nMismoAtaque > 1)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                }
                // --- EMBESTIDA LARGA ---
                else
                {
                    if (estadoActual == EstadoMarrajo.EmbestidaLarga)
                        nMismoAtaque++;
                    else 
                        nMismoAtaque = 1;

                    // Máximo se repite 1 vez
                    if (nMismoAtaque > 1)
                    {
                        nMismoAtaque = 1;
                        yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                    }
                    else
                    {
                        yield return StartCoroutine(AtaqueEmbestidaLarga());
                    }
                }
            }

            // Pausa entre decisiones
            yield return new WaitForSeconds(0.2f);
        }

        Morir();
    }

    private IEnumerator SalirDePantalla()
    {
        estadoActual = EstadoMarrajo.FueraDeCamara;

        Vector2 direccionHuida = (transform.position - jugador.position).normalized;
        if (direccionHuida == Vector2.zero) direccionHuida = transform.up;

        Camera cam = Camera.main;
        float altoCamara = cam.orthographicSize;
        float anchoCamara = cam.orthographicSize * cam.aspect;
        Vector3 centroCamara = cam.transform.position;

        float margen = 4f; 

        while (Mathf.Abs(transform.position.x - centroCamara.x) < (anchoCamara + margen) &&
            Mathf.Abs(transform.position.y - centroCamara.y) < (altoCamara + margen))
        {
            rb.linearVelocity = direccionHuida * velocidadSalida;
            GirarSprite(direccionHuida);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero; 
    }

    private IEnumerator AtaqueSierra()
    {
        estadoActual = EstadoMarrajo.Sierra;
        if (faseActual > 1) StartCoroutine(SpawnearCirculosExplosivos());
        yield return new WaitForSeconds(0.6f);
        
        if (hitboxSierra != null) hitboxSierra.SetActive(true);

        float T = 0f;
        while (T < tiempoSierra)
        {
            transform.Rotate(0f, 0f, velocidadRotacionSierra * Time.deltaTime);
            T += Time.deltaTime;
            yield return null;
        }

        transform.rotation = Quaternion.identity;
        if (hitboxSierra != null) hitboxSierra.SetActive(false);
        // nada q ver pero sabeis me raya mazo q en c# pongan la llave debajo yo no hago eso nunca pero me lo hace automatico y si lo pusiera como lo pongo yo siempre estaria mezclado y eso seria peor asi q terribles destinos esperan a los malparados
    }

    private IEnumerator SpawnearCirculosExplosivos()
    {
        float intervalo = tiempoSierra / cantidadCirculosSierra;
        Camera cam = Camera.main;

        if (cam != null)
        {
            // 1. Calculamos los límites visibles de la cámara
            float altoCamara = cam.orthographicSize;
            float anchoCamara = altoCamara * cam.aspect;
            Vector3 centroCamara = cam.transform.position;

            // Margen de seguridad para que el radio del círculo no sobresalga del borde de la pantalla
            float margen = 1.5f; 

            float minX = centroCamara.x - anchoCamara + margen;
            float maxX = centroCamara.x + anchoCamara - margen;
            float minY = centroCamara.y - altoCamara + margen;
            float maxY = centroCamara.y + altoCamara - margen;

            // 2. Generamos los círculos dentro de esos límites
            for (int i = 0; i < cantidadCirculosSierra; i++)
            {
                if (prefabCirculoExplosivo != null)
                {
                    Vector3 posSpawn;

                    // El 50% persigue la posición del jugador (limitado a la cámara)
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        Vector3 posJugador = jugador.position + (Vector3)(UnityEngine.Random.insideUnitSphere * 10f);
                        posSpawn = new Vector3(
                            Mathf.Clamp(posJugador.x, minX, maxX),
                            Mathf.Clamp(posJugador.y, minY, maxY),
                            0f
                        );
                    }
                    else
                    {
                        // El otro 50% en una posición totalmente aleatoria dentro de la vista de la cámara
                        posSpawn = new Vector3(
                            UnityEngine.Random.Range(minX, maxX),
                            UnityEngine.Random.Range(minY, maxY),
                            0f
                        );
                    }

                    Instantiate(prefabCirculoExplosivo, posSpawn, Quaternion.identity);
                }

                yield return new WaitForSeconds(intervalo);
            }
        }
    }

    private IEnumerator AtaqueEmbestidaexplosiva()
    {
        estadoActual = EstadoMarrajo.EmbestidaExplosivo;
        haChocadoPared = false;

        Vector2 direccion = (jugador.position - transform.position).normalized;
        GirarSprite(direccion);

        if (lineaTelegrafiado != null)
        {
            lineaTelegrafiado.enabled = true;
            lineaTelegrafiado.SetPosition(0, transform.position);
            lineaTelegrafiado.SetPosition(1, (Vector2)transform.position + (direccion * 16f));
        }
        yield return new WaitForSeconds(0.4f);
        if (lineaTelegrafiado != null) lineaTelegrafiado.enabled = false;
        if (hitboxBoca != null) hitboxBoca.SetActive(true);

        StartCoroutine(ImagenesResiduales());

        rb.linearVelocity = direccion * velocidadEmbestidaExplosiva;
        if (colisionPared != null) colisionPared.SetActive(true);

        // Avance con temporizador de seguridad por si no toca pared
        float tMax = 1.2f;
        float t = 0f;
        while (!haChocadoPared && t < tMax)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!haChocadoPared)
        {
            rb.linearVelocity = Vector2.zero;
            if (colisionPared != null) colisionPared.SetActive(false);
            if (hitboxBoca != null) hitboxBoca.SetActive(false);
        }
        else
        {
            // Si choco, esperamos a que el Stun de OnTriggerEnter2D termine
            while (enStun) yield return null;
        }
    }

    private IEnumerator AtaqueEmbestidaLarga()
    {
        yield return StartCoroutine(SalirDePantalla());

        estadoActual = EstadoMarrajo.EmbestidaLarga;
        nEmbestidas = UnityEngine.Random.Range(1, 4); // gente como se ponen emotes en fedora?

        for (int i = 0; i < nEmbestidas; i++)
        {
            if (faseActual == 2)
                for (int e = 0; e < 2; e++)
                {
                    Hijos[e].UnaEmbestida();
                }

            PuntoEmbestida P = puntosDeEntrada[UnityEngine.Random.Range(0, puntosDeEntrada.Length)];
            transform.position = P.punto.position;

            Vector2 dirEmbestida = P.direcciones[UnityEngine.Random.Range(0, P.direcciones.Length)];
            GirarSprite(dirEmbestida);

            if (lineaTelegrafiado != null)
            {
                lineaTelegrafiado.enabled = true;
                lineaTelegrafiado.SetPosition(0, transform.position);
                lineaTelegrafiado.SetPosition(1, (Vector2)transform.position + (dirEmbestida * 30f));
            }

            yield return new WaitForSeconds(0.4f);
            if (lineaTelegrafiado != null) lineaTelegrafiado.enabled = false;
            if (hitboxBoca != null) hitboxBoca.SetActive(true);

            StartCoroutine(ImagenesResiduales());

            if (i < nEmbestidas - 1)
            {
                float tiempoPasada = 0f;
                float duracionPasada = 0.8f;

                while (tiempoPasada < duracionPasada)
                {
                    rb.linearVelocity = dirEmbestida * velocidadEmbestidaLarga;
                    tiempoPasada += Time.deltaTime;
                    yield return null;
                }

                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(0.2f);
            } 
            else
            {
                // Última embestida -> Busca la pared
                haChocadoPared = false;
                rb.linearVelocity = dirEmbestida * velocidadEmbestidaLarga;
                yield return new WaitForSeconds(0.2f); // da tiempo a entrar a la sala

                if (colisionPared != null) colisionPared.SetActive(true);

                // Espera con tiempo límite de 2.5s para evitar que se pille si no choca
                float tMax = 2.5f;
                float t = 0f;
                while (!haChocadoPared && t < tMax)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                if (enStun)
                {
                    while (enStun) yield return null;
                }
                if (hitboxBoca != null) hitboxBoca.SetActive(false);
            }
        }
    }

    private IEnumerator Frenazo()
    {
        enStun = true;
        estadoActual = EstadoMarrajo.Frenazo;
        rb.linearVelocity = Vector2.zero;
        if (colisionPared != null) colisionPared.SetActive(false);

        yield return new WaitForSeconds(tiempoFrenazo);

        transform.rotation = Quaternion.identity; // Se recoloca tras el aturdimiento
        enStun = false;
    }

    private IEnumerator ImagenesResiduales()
    {
        while (estadoActual == EstadoMarrajo.EmbestidaLarga || estadoActual == EstadoMarrajo.EmbestidaExplosivo)
        {
            GameObject fantasma = new GameObject("EcoDash_Clon");
            EcoDash componenteEco = fantasma.AddComponent<EcoDash>();

            componenteEco.Ecos(
                spriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                Color.blue,
                0.4f,
                spriteRenderer.sortingOrder - 1
            );

            yield return new WaitForSeconds(0.08f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (estadoActual == EstadoMarrajo.EmbestidaLarga || estadoActual == EstadoMarrajo.EmbestidaExplosivo)
        {
            if (!collision.isTrigger && collision.gameObject.layer == LayerMask.NameToLayer("Salas"))
            {
                haChocadoPared = true;
                StartCoroutine(Frenazo());
            }
        }
    }

    private void GirarSprite(Vector2 direccion)
    {
        if (direccion.sqrMagnitude < 0.001f) return;

        float anguloZ = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        Vector3 escala = transform.localScale;
        escala.x = (direccion.x < 0) ? Mathf.Abs(escala.x) : -Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (direccion.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    public void RecibirDano(float dano)
    {
        vidaActual -= dano;

        if (vidaActual <= vidaMaxima * 0.33f) 
        {
            faseActual = 3;

            for (int e = 0; e < Hijos.Length; e++)
            {
                Hijos[e].Independizar();
            }
        }
        else if (vidaActual <= vidaMaxima * 0.66f) faseActual = 2;

        if (vidaActual <= 0)
        {
            StopAllCoroutines();
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        vidaActual += cantidad;
        if (vidaActual > vidaMaxima) vidaActual = vidaMaxima;
    }

    public void Morir()
    {
        if (muerteNotificada) return;
        muerteNotificada = true;
        Debug.Log("El Boss Marrajo ha sido derrotado.");
        OnBossMuerto?.Invoke();
        foreach (MiniMako hijo in Hijos)
        {
            if (hijo != null)
            {
                Destroy(hijo.gameObject);
            }
        }
        Destroy(gameObject);
    }
}