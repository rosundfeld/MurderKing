using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
   [SerializeField] private MeshRenderer headMeshRenderer;
   [SerializeField] private MeshRenderer bodyMeshRenderer;

   private Material material;

   private void Awake()
    {
        //Clona o material para que cada jogador tenha seu próprio material independente
        material = new Material(headMeshRenderer.material);
        headMeshRenderer.material = material;
        bodyMeshRenderer.material = material;
    }

    public void SetPlayerColor(Color color)
    {
        material.color = color;
    }
}
