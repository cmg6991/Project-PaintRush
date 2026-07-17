using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프로젝트에서 사용하는 물감 색상 정보를 관리합니다.
/// 새 색을 추가할 때 MonsterAI의 switch 문을 수정하지 않고,
/// ElementType과 이 카탈로그 항목만 추가하면 됩니다.
/// </summary>
[CreateAssetMenu(
    fileName = "PaintColorCatalog",
    menuName = "PaintRush/Paint Color Catalog")]
public class PaintColorCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [SerializeField] private string id = "Red";
        [SerializeField] private ElementType element = ElementType.None;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private GameObject dropPrefab;

        [Header("몬스터 속도 배율")]
        [SerializeField, Min(0f)] private float patrolSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float chaseSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float runAwaySpeedMultiplier = 1f;

        public string Id => id;
        public ElementType Element => element;
        public Color Color => color;
        public GameObject DropPrefab => dropPrefab;
        public float PatrolSpeedMultiplier => patrolSpeedMultiplier;
        public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
        public float RunAwaySpeedMultiplier => runAwaySpeedMultiplier;

        public void NormalizeId()
        {
            id = string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim();
        }
    }

    [SerializeField] private List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryResolveElement(
        Color inputColor,
        float tolerance,
        out ElementType element)
    {
        Entry closestEntry = null;
        float closestDistance = float.MaxValue;

        foreach (Entry entry in entries)
        {
            if (entry == null || entry.Element == ElementType.None) { continue; }

            float distance = ColorDistance(inputColor, entry.Color);

            if (distance >= closestDistance) { continue; }

            closestEntry = entry;
            closestDistance = distance;
        }

        if (closestEntry == null || closestDistance > tolerance)
        {
            element = ElementType.None;
            return false;
        }

        element = closestEntry.Element;
        return true;
    }

    public bool TryGetEntry(ElementType element, out Entry result)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.Element == element)
            {
                result = entry;
                return true;
            }
        }

        result = null;
        return false;
    }

    public bool TryGetColor(ElementType element, out Color color)
    {
        if (TryGetEntry(element, out Entry entry))
        {
            color = entry.Color;
            return true;
        }

        color = Color.white;
        return false;
    }

    public bool TryGetDropPrefab(
        ElementType element,
        out GameObject dropPrefab)
    {
        if (TryGetEntry(element, out Entry entry))
        {
            dropPrefab = entry.DropPrefab;
            return dropPrefab != null;
        }

        dropPrefab = null;
        return false;
    }

    public bool TryGetId(ElementType element, out string id)
    {
        if (TryGetEntry(element, out Entry entry))
        {
            id = entry.Id;
            return !string.IsNullOrEmpty(id);
        }

        id = string.Empty;
        return false;
    }

    public static float ColorDistance(Color first, Color second)
    {
        Vector3 difference = new(
            first.r - second.r,
            first.g - second.g,
            first.b - second.b);

        return difference.magnitude;
    }

    private void OnValidate()
    {
        HashSet<string> usedIds =
            new(StringComparer.OrdinalIgnoreCase);

        HashSet<ElementType> usedElements = new();

        foreach (Entry entry in entries)
        {
            if (entry == null) { continue; }

            entry.NormalizeId();

            if (!string.IsNullOrEmpty(entry.Id) &&
                !usedIds.Add(entry.Id)) {
                Debug.LogWarning( $"{name}: 중복된 색상 ID가 있습니다. ({entry.Id})", this);
            }

            if (entry.Element != ElementType.None &&
                !usedElements.Add(entry.Element)) {
                Debug.LogWarning( $"{name}: 중복된 ElementType이 있습니다. ({entry.Element})", this);
            }
        }
    }
}
