using System.Collections;
using UnityEngine;

public class KHS_Script_SlotMachineController : MonoBehaviour
{
    [Header("���Ըӽ� �귿����")]
    [Tooltip("���Ըӽ��� ���� �迭")]
    [SerializeField] private int[] numberLists = new int[3];

    [Header("���ھ� �Ŵ��� ����")]
    [SerializeField] private KHS_Script_ScoreManager scoreManager; // ���� �Ŵ���

    // ���� ���� ����
    private bool isSMActive = true; // ���Ըӽ��� ���� �޾Ƶ��� �� �ִ� �������� Ȯ��

    private void Awake()
    {
        // �����Ϳ��� ���� ���� �� ���� ��� �ڵ� Ž��
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    private void OnTriggerEnter(Collider _collider)
    {
        if(_collider.CompareTag("Ball") && isSMActive)
        {
            Rigidbody ballRb = _collider.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                StartCoroutine(SlotMachineSequence(ballRb));
            }
        }
    }

    private IEnumerator SlotMachineSequence(Rigidbody _rb)
    {
        isSMActive = false;

        // 1. �Ի簢(�ӵ� ����)�� ����ϰ� ���� ���������� ���� �� ����
        Vector3 incomingVelocity = _rb.linearVelocity;
        _rb.isKinematic = true;
        _rb.gameObject.SetActive(false);


        // 2. ���Ըӽ� ����� ���� ������ ��� �� ����
        float spinDuration = 4.0f; // ���� ȸ�� �� �ð�
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            for (int i = 0; i < numberLists.Length; i++)
            {
                numberLists[i] = Random.Range(1,4); // 1~4 ����
            }

            // ����׿����� ȸ�� �� ���� ��� (���û���)
            Debug.Log($"Spinning... {numberLists[0]} {numberLists[1]} {numberLists[2]}");

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        // ���� ��� Ȯ��
        for (int i = 0; i < numberLists.Length; i++)
        {
            numberLists[i] = Random.Range(1, 4);
        }

        // ��� ���
        Debug.LogWarning($"���Ըӽ� ���: {numberLists[0]} | {numberLists[1]} | {numberLists[2]}");

        // 3. ���� ��� �� ����
        int score = CalculateSlotScore(numberLists[0], numberLists[1], numberLists[2]);
        if (scoreManager != null)
        {
            scoreManager.AddScore(score);
            Debug.Log($"�� ȹ�� ����: {score}");
        }
        else
        {
            Debug.LogError("ScoreManager�� ã�� �� �����ϴ�!");
        }


        // 4. ���� ����� ���� ȿ���� �ٽ� Ȱ��ȭ�ϰ�, �ݻ纤�� �������� �߻�
        _rb.gameObject.SetActive(true);
        _rb.isKinematic = false;
        _rb.linearVelocity = - incomingVelocity;

        // 5. ª�� �ð� �� ���Ըӽ��� �ٽ� Ȱ��ȭ (���� ������ ��� �ð� Ȯ��)
        yield return new WaitForSeconds(0.5f);
        isSMActive = true;

    }
    private int CalculateSlotScore(int a, int b, int c)
    {
        if (a == b && b == c)
            return 5000;
        else if (a == b || b == c || a == c)
            return 1000;
        else
            return 500;
    }
}
