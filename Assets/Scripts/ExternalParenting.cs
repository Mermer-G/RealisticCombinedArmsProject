using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExternalParenting : MonoBehaviour
{
    public Transform parentObject; // Ebeveyn obje referans�
    public Transform[] childObjects; // �ocuk objelerin referanslar�

    private Vector3[] initialChildPositions; // �ocuk objelerin ba�lang��ta ebeveyn objeye g�re pozisyonlar�
    private Quaternion previousParentRotation; // Ebeveynin �nceki rotasyonu

    private void Start()
    {
        // �ocuk objelerin ba�lang��taki pozisyonlar�n� ebeveyn objeye g�re kaydet
        UpdateInitialPositions();

        // Ebeveynin ba�lang�� rotasyonunu sakla
        previousParentRotation = parentObject.rotation;
    }

    private void Update()
    {
        // �ocuk objeleri ebeveynin rotasyonuna g�re do�ru bir �ekilde d�nd�r
        RotateChildrenWithParent();
    }

    private void UpdateInitialPositions()
    {
        initialChildPositions = new Vector3[childObjects.Length];
        for (int i = 0; i < childObjects.Length; i++)
        {
            // �ocuk objenin pozisyonunu ebeveynin lokal uzay�na d�n��t�r
            initialChildPositions[i] = parentObject.InverseTransformPoint(childObjects[i].position);
        }
    }

    private void RotateChildrenWithParent()
    {
        // Ebeveynin mevcut rotasyonunu al
        Quaternion currentParentRotation = parentObject.rotation;

        // Ebeveynin �nceki rotasyonuna g�re yap�lan de�i�ikli�i hesapla
        Quaternion rotationDifference = currentParentRotation * Quaternion.Inverse(previousParentRotation);

        // Her bir �ocuk objenin konumunu ve rotasyonunu bu de�i�ikli�e g�re g�ncelle
        for (int i = 0; i < childObjects.Length; i++)
        {
            // �ocu�un mevcut pozisyonunu ebeveynin de�i�ikli�ine g�re g�ncelle
            Vector3 offsetPosition = initialChildPositions[i];
            Vector3 worldPosition = parentObject.TransformPoint(offsetPosition);
            Vector3 newPosition = rotationDifference * (childObjects[i].position - parentObject.position) + parentObject.position;
            childObjects[i].position = newPosition;

            // �ocu�un rotasyonunu ebeveynin yapt��� de�i�ikli�e ekle
            childObjects[i].rotation = rotationDifference * childObjects[i].rotation;
        }

        // Ebeveynin �nceki rotasyonunu g�ncelle
        previousParentRotation = currentParentRotation;
    }
}
