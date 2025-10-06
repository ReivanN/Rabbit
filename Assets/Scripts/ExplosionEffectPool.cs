using System.Collections.Generic;
using UnityEngine;
using YG;

public class ExplosionEffectPool : MonoBehaviour
{
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private Transform poolContainer;
    
    private Queue<GameObject> explosionPool = new Queue<GameObject>();
    
    public static ExplosionEffectPool Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateExplosionInPool();
        }
    }

    private void CreateExplosionInPool()
    {
        var effect = Instantiate(explosionEffectPrefab, poolContainer);
        effect.SetActive(false);
        
        // Предварительно проинициализируем ParticleSystem
        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        explosionPool.Enqueue(effect);
        YG2.InterstitialAdvShow();
    }

    public GameObject GetExplosionEffect(Vector3 position)
    {
        if (explosionPool.Count == 0)
        {
            CreateExplosionInPool();
        }

        var effect = explosionPool.Dequeue();
        effect.transform.position = position;
        effect.SetActive(true);
        
        // Запускаем партиклы
        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play(true);
        }
        
        return effect;
    }

    public void ReturnExplosionEffect(GameObject effect)
    {
        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        effect.SetActive(false);
        explosionPool.Enqueue(effect);
    }
}