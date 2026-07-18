using Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class TreeScrollController : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private UnlockNodeUI[] _allSlots;
    [SerializeField] private UnlockNodeData _rootNode;
    private Coroutine _scrollRoutine;
    private void Start()
    {
        ScrollToFrontier();
    }
    private List<UnlockNodeData> GetChildren(UnlockNodeData node)
    {
        List<UnlockNodeData> children = new List<UnlockNodeData>();

        foreach (UnlockNodeUI slot in _allSlots)
        {
            if (slot.NodeData.prerequisites.Contains(node))
            {
                children.Add(slot.NodeData);
            }
        }

        return children;
    }
    // DFS(깊이 우선 탐색): Root부터 자식 방향으로 계속 파고들면서,
    // 언락된 노드들 중 트리에서 가장 깊은(=가장 마지막으로 도달한) 지점을 찾는다.
    private UnlockNodeData FindDeepestUnlockedNode(UnlockNodeData node, HashSet<UnlockNodeData> visited)
    {
        if (node == null || !UIManager.Instance.IsUnlocked(node)) return null;

        // Final처럼 부모가 2개(A, B)인 노드는 DFS 도중 두 번 걸릴 수 있어서, 방문 기록으로 중복을 막는다.
        if (!visited.Add(node)) return node;

        UnlockNodeData deepest = node;

        foreach (UnlockNodeData child in GetChildren(node))
        {
            UnlockNodeData result = FindDeepestUnlockedNode(child, visited); // 자식 쪽으로 재귀 호출 (DFS)
            if (result != null)
            {
                deepest = result;
            }
        }

        return deepest;
    }

    //    //지금 노드가 언락 안 됐으면 null 반환 — 여기서부터는 더 이상 파고들 이유가 없으니 가지치기
    //    언락됐으면 일단 deepest = node로 자기 자신을 후보로 잡아두고, 자식들한테 재귀
    //    자식 쪽에서 더 깊은(더 나중에 언락된 쪽) 결과가 나오면 deepest를 그걸로 갱신
    //    결국 맨 위(Root)에서 이 함수를 부르면, 언락된 상태를 따라 트리를 끝까지 타고 내려간 "가장 마지막 언락 지점"이 반환.
    //
    private RectTransform FindSlotRect(UnlockNodeData node)
    {
        foreach (UnlockNodeUI slot in _allSlots)
        {
            if (slot.NodeData == node)
            {
                return slot.transform as RectTransform;
            }
        }

        return null;
    }

    private void ScrollTo(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        float scrollableHeight = _content.rect.height - _scrollRect.viewport.rect.height;
        if (scrollableHeight <= 0f) return; // 콘텐츠가 뷰포트보다 작으면 스크롤할 필요 자체가 없음

        float targetY = _content.InverseTransformPoint(target.position).y;
        float lowerBound = _content.rect.yMax - scrollableHeight;
        float normalizedY = Mathf.InverseLerp(lowerBound, _content.rect.yMax, targetY);
        float clampedTarget = Mathf.Clamp01(normalizedY);

        if (_scrollRoutine != null)
        {
            StopCoroutine(_scrollRoutine);
        }
        _scrollRoutine = StartCoroutine(SmoothScrollTo(clampedTarget));
    }
    public void ScrollToFrontier()
    {
        UnlockNodeData deepest = FindDeepestUnlockedNode(_rootNode, new HashSet<UnlockNodeData>());
        if (deepest == null) return;

        RectTransform targetSlot = FindSlotRect(deepest);
        if (targetSlot == null) return;

        ScrollTo(targetSlot);
    }
    private IEnumerator SmoothScrollTo(float target)
    {
        float start = _scrollRect.verticalNormalizedPosition;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, t);
            yield return null;
        }

        _scrollRect.verticalNormalizedPosition = target;
    }
}