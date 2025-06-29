using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExternalParenting : MonoBehaviour
{
    public Transform parentObject; // Ebeveyn obje referansý
    public Transform[] childObjects; // Çocuk objelerin referanslarý

    private Vector3[] initialChildPositions; // Çocuk objelerin baþlangýçta ebeveyn objeye göre pozisyonlarý
    private Quaternion previousParentRotation; // Ebeveynin önceki rotasyonu

    private void Start()
    {
        // Çocuk objelerin baþlangýçtaki pozisyonlarýný ebeveyn objeye göre kaydet
        UpdateInitialPositions();

        // Ebeveynin baþlangýç rotasyonunu sakla
        previousParentRotation = parentObject.rotation;
    }

    private void Update()
    {
        // Çocuk objeleri ebeveynin rotasyonuna göre doðru bir þekilde döndür
        RotateChildrenWithParent();
    }

    private void UpdateInitialPositions()
    {
        initialChildPositions = new Vector3[childObjects.Length];
        for (int i = 0; i < childObjects.Length; i++)
        {
            // Çocuk objenin pozisyonunu ebeveynin lokal uzayýna dönüþtür
            initialChildPositions[i] = parentObject.InverseTransformPoint(childObjects[i].position);
        }
    }

    private void RotateChildrenWithParent()
    {
        // Ebeveynin mevcut rotasyonunu al
        Quaternion currentParentRotation = parentObject.rotation;

        // Ebeveynin önceki rotasyonuna göre yapýlan deðiþikliði hesapla
        Quaternion rotationDifference = currentParentRotation * Quaternion.Inverse(previousParentRotation);

        // Her bir çocuk objenin konumunu ve rotasyonunu bu deðiþikliðe göre güncelle
        for (int i = 0; i < childObjects.Length; i++)
        {
            // Çocuðun mevcut pozisyonunu ebeveynin deðiþikliðine göre güncelle
            Vector3 offsetPosition = initialChildPositions[i];
            Vector3 worldPosition = parentObject.TransformPoint(offsetPosition);
            Vector3 newPosition = rotationDifference * (childObjects[i].position - parentObject.position) + parentObject.position;
            childObjects[i].position = newPosition;

            // Çocuðun rotasyonunu ebeveynin yaptýðý deðiþikliðe ekle
            childObjects[i].rotation = rotationDifference * childObjects[i].rotation;
        }

        // Ebeveynin önceki rotasyonunu güncelle
        previousParentRotation = currentParentRotation;
    }
}
