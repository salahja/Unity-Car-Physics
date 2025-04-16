using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

public class EndlessLevelHandler : MonoBehaviour
{
    [SerializeField] GameObject[] sectionsPool = new GameObject[20];
    Transform playerCarTransform;
    GameObject[] sections = new GameObject[10];
    WaitForSeconds waitFor100ms = new WaitForSeconds(0.1f);
    const float sectionLength = 26f;

    [Header("References")]
    [SerializeField] GameObject[] sectionPrefabs;
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] Transform playerCar;
    [SerializeField] float roadYPosition = 0f; // Added this to control road height

    void Start()
    {
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;
        InitializePool();
        GenerateInitialSections();
        StartCoroutine(UpdateLessOften());
    }

    void InitializePool()
    {
        int prefabIndex = 0;
        for (int i = 0; i < sectionsPool.Length; i++)
        {
            sectionsPool[i] = Instantiate(sectionPrefabs[prefabIndex]);
            sectionsPool[i].SetActive(false);
            prefabIndex++;
            if (prefabIndex > sectionPrefabs.Length - 1)
                prefabIndex = 0;
        }
    }

    void GenerateInitialSections()
    {
        for (int i = 0; i < sections.Length; i++)
        {
            GameObject randomSection = GetRandomSectionFromPool();
            randomSection.transform.position = new Vector3(
                sectionsPool[1].transform.position.x, 
                roadYPosition, // Using the configurable Y position
                i * sectionLength
            );
            randomSection.SetActive(true);
            sections[i] = randomSection;
        }
    }

    IEnumerator UpdateLessOften()
    {
        while (true)
        {
            yield return waitFor100ms;
            UpdateSectionPositions();
        }
    }

    void UpdateSectionPositions()
    {
        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i].transform.position.z - playerCarTransform.position.z < -sectionLength)
            {
                Vector3 lastSectionPosition = sections[i].transform.position;
                sections[i].SetActive(false);
                sections[i] = GetRandomSectionFromPool();
                sections[i].transform.position = new Vector3(
                    lastSectionPosition.x, 
                    roadYPosition, // Consistent Y position
                    lastSectionPosition.z + sectionLength * sections.Length
                );
                sections[i].SetActive(true);
            }
        }
    }

    GameObject GetRandomSectionFromPool()
    {
        int randomIndex = Random.Range(0, sectionsPool.Length);
        bool isNewSectionFound = false;
        
        while (!isNewSectionFound)
        {
            if (!sectionsPool[randomIndex].activeInHierarchy)
            {
                isNewSectionFound = true;
            }
            else
            {
                randomIndex++;
                if (randomIndex >= sectionsPool.Length)
                {
                    randomIndex = 0;
                }
            }
        }
        return sectionsPool[randomIndex];
    }
}