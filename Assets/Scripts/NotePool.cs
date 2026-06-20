using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class NotePool : MonoBehaviour
{
    private ObjectPool<PooledNote> pool;
    private GameObject prefab;
    private Transform poolRoot;

    public bool IsInitialized => pool != null;

    public void Initialize(GameObject notePrefab, int initialSize, int maxSize)
    {
        if (IsInitialized || notePrefab == null)
        {
            return;
        }

        prefab = notePrefab;
        initialSize = Mathf.Max(0, initialSize);
        maxSize = Mathf.Max(1, initialSize, maxSize);

        GameObject root = new GameObject("Note Pool");
        root.transform.SetParent(transform, false);
        poolRoot = root.transform;

        pool = new ObjectPool<PooledNote>(
            CreateNote,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPooledNote,
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxSize);

        Prewarm(initialSize);
    }

    public PooledNote Get(Vector3 position, Quaternion rotation)
    {
        PooledNote note = pool.Get();
        note.transform.SetParent(null, true);
        note.transform.SetPositionAndRotation(position, rotation);
        return note;
    }

    internal void Release(PooledNote note)
    {
        if (pool != null && note != null)
        {
            pool.Release(note);
        }
    }

    private PooledNote CreateNote()
    {
        GameObject instance = Instantiate(prefab, poolRoot);
        instance.name = prefab.name;

        PooledNote pooledNote = instance.GetComponent<PooledNote>();
        if (pooledNote == null)
        {
            pooledNote = instance.AddComponent<PooledNote>();
        }

        pooledNote.Bind(this);
        instance.SetActive(false);
        return pooledNote;
    }

    private static void OnTakeFromPool(PooledNote note)
    {
        note.PrepareForUse();
    }

    private void OnReturnedToPool(PooledNote note)
    {
        note.PrepareForPool();
        note.transform.SetParent(poolRoot, false);
    }

    private static void OnDestroyPooledNote(PooledNote note)
    {
        if (note != null)
        {
            Destroy(note.gameObject);
        }
    }

    private void Prewarm(int count)
    {
        if (count <= 0)
        {
            return;
        }

        List<PooledNote> notes = new List<PooledNote>(count);
        for (int i = 0; i < count; i++)
        {
            notes.Add(pool.Get());
        }

        for (int i = 0; i < notes.Count; i++)
        {
            pool.Release(notes[i]);
        }
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }
}

public sealed class PooledNote : MonoBehaviour
{
    private NotePool owner;
    private TimedNoteMover timedNoteMover;
    private ParticleSystem[] particleSystems;
    private bool isInPool = true;

    public TimedNoteMover Mover => timedNoteMover;

    internal void Bind(NotePool notePool)
    {
        owner = notePool;
        timedNoteMover = GetComponent<TimedNoteMover>();
        if (timedNoteMover == null)
        {
            timedNoteMover = gameObject.AddComponent<TimedNoteMover>();
        }

        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    internal void PrepareForUse()
    {
        isInPool = false;
        gameObject.SetActive(true);
        timedNoteMover.enabled = true;
    }

    internal void PrepareForPool()
    {
        isInPool = true;
        timedNoteMover.StopMovement();

        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        gameObject.SetActive(false);
    }

    public void ReturnToPool()
    {
        if (isInPool)
        {
            return;
        }

        isInPool = true;
        owner.Release(this);
    }

    public static void ReturnOrDestroy(GameObject note)
    {
        if (note != null && note.TryGetComponent(out PooledNote pooledNote))
        {
            pooledNote.ReturnToPool();
            return;
        }

        Destroy(note);
    }
}
