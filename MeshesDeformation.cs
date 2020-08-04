using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshesDeformation : MonoBehaviour
{
    [SerializeField] private MeshFilter[] meshFilters = default;
    [SerializeField] private MeshCollider[] colliders = default;
    [SerializeField] private float impactDamage = 1f;
    [SerializeField] private float deformationRadius = 0.5f;
    [SerializeField] private float maxDeformation = 0.5f;
    [SerializeField] private float minVelocity = 2f;
    private float delayTimeDeform = 0.1f;
    private float minVertsDistanceToRestore = 0.002f;
    private float vertsRestoreSpeed = 2f;
    private Vector3[][] originalVertices;
    private float nextTimeDeform = 0f;   
    private bool isRepairing = false;
    private bool isRepaired = false;

	private void Start()
    {
        originalVertices = new Vector3[meshFilters.Length][];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            originalVertices[i] = meshFilters[i].mesh.vertices;
            meshFilters[i].mesh.MarkDynamic();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isRepairing)
            {
                isRepairing = true;
            }
        }

        RestoreMesh();
    }

    private void DeformationMesh(Mesh mesh, Transform localTransform, Vector3 contactPoint, Vector3 contactVelocity, int i)
    {
        bool hasDeformated = false;
        
        Vector3 localContactPoint = localTransform.InverseTransformPoint(contactPoint);
        Vector3 localContactForce = localTransform.InverseTransformDirection(contactVelocity);
        Vector3[] vertices = mesh.vertices;

        for (int j = 0; j < vertices.Length; j++)
        {
            float distance = (localContactPoint - vertices[j]).magnitude;

            if (distance <= deformationRadius)
            {
                vertices[j] += localContactForce * (deformationRadius - distance) * impactDamage;
                Vector3 deformation = vertices[j] - originalVertices[i][j];

                if (deformation.magnitude > maxDeformation)
                {
                    vertices[j] = originalVertices[i][j] + deformation.normalized * maxDeformation;
                }

                hasDeformated = true;
            }
        }

        if (hasDeformated)
        {
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (colliders.Length > 0)
            {
                if (colliders[i] != null)
                {
                    colliders[i].sharedMesh = mesh;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time > nextTimeDeform)
        {
            if (collision.relativeVelocity.magnitude > minVelocity)
            {
                isRepaired = false;
                
                Vector3 contactPoint = collision.contacts[0].point;
                Vector3 contactVelocity = collision.relativeVelocity * 0.02f;

                for (int i = 0; i < meshFilters.Length; i++)
                {
                    if (meshFilters[i] != null)
                    {
                        DeformationMesh(meshFilters[i].mesh, meshFilters[i].transform, contactPoint, contactVelocity, i);
                    }
                }

                nextTimeDeform = Time.time + delayTimeDeform;
            }
        }
    }

    private void RestoreMesh()
    {
        if (!isRepaired && isRepairing)
        {
            isRepaired = true;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                Mesh mesh = meshFilters[i].mesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] origVerts = originalVertices[i];

                for (int j = 0; j < vertices.Length; j++)
                {
                    vertices[j] += (origVerts[j] - vertices[j]) * Time.deltaTime * vertsRestoreSpeed;

                    if ((origVerts[j] - vertices[j]).magnitude > minVertsDistanceToRestore)
                    {
                        isRepaired = false;
                    }
                }

                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                if (colliders[i] != null)
                {
                    colliders[i].sharedMesh = mesh;
                }
            }

            if (isRepaired)
            {
                isRepairing = false;

                for (int i = 0; i < meshFilters.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].sharedMesh = meshFilters[i].mesh;
                    }
                }
            }
        }
    }
}
