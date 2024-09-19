using Autodesk.Fbx;
using UnityEngine;
using System.Collections.Generic;

public class fbxRuntimeExporter: MonoBehaviour {
	public void ExportSelectedObjects(string filePath, List<GameObject> objectsToExport) {
		using (FbxManager fbxManager = FbxManager.Create()) {
			// Configure IO settings
			fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

			using (FbxExporter exporter = FbxExporter.Create(fbxManager, "myExporter")) {
				// Initialize the exporter
				if (!exporter.Initialize(filePath, -1, fbxManager.GetIOSettings())) {
					Debug.LogError("Failed to initialize the exporter.");
					return;
				}

				// Create a new scene to export
				FbxScene scene = FbxScene.Create(fbxManager, "myScene");

				foreach (GameObject obj in objectsToExport) {
					FbxNode fbxNode = CreateFbxNodeFromGameObject(fbxManager, obj);
					if (fbxNode != null) {
						scene.GetRootNode().AddChild(fbxNode);
					}
				}

				// Export the scene to the file
				exporter.Export(scene);
				Debug.Log("Exported to " + filePath);
			}
		}
	}


	private FbxNode CreateFbxNodeFromGameObject(FbxManager fbxManager, GameObject unityObject) {
		FbxNode fbxNode = FbxNode.Create(fbxManager, unityObject.name);

		// Set transformations
		fbxNode.LclTranslation.Set(new FbxDouble3(unityObject.transform.position.x,
												   unityObject.transform.position.y,
												   unityObject.transform.position.z));
		fbxNode.LclRotation.Set(new FbxDouble3(unityObject.transform.eulerAngles.x,
												unityObject.transform.eulerAngles.y,
												unityObject.transform.eulerAngles.z));
		fbxNode.LclScaling.Set(new FbxDouble3(unityObject.transform.localScale.x,
											   unityObject.transform.localScale.y,
											   unityObject.transform.localScale.z));

		// Handle mesh
		MeshFilter meshFilter = unityObject.GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null) {
			// Convert Unity mesh to FBX mesh
			FbxMesh fbxMesh = ConvertUnityMeshToFbxMesh(fbxManager, meshFilter.sharedMesh);

			// Create a new node for the mesh
			FbxNode meshNode = FbxNode.Create(fbxManager, fbxMesh.GetName());
			meshNode.SetNodeAttribute(fbxMesh); // Set the mesh

			// Add the mesh node to the parent node
			fbxNode.AddChild(meshNode);
		}

		return fbxNode;
	}

	private FbxMesh ConvertUnityMeshToFbxMesh(FbxManager fbxManager, Mesh unityMesh) {
		FbxMesh fbxMesh = FbxMesh.Create(fbxManager, unityMesh.name);
		float scaleFactor = 100.0f; // Adjust this value as needed

		// Set vertex positions
		Vector3[] vertices = unityMesh.vertices;
		Debug.Log($"Mesh: {unityMesh.name}, Vertices: {vertices.Length}, Triangles: {unityMesh.triangles.Length}");

		fbxMesh.InitControlPoints(vertices.Length);
		for (int i = 0; i < vertices.Length; i++) {
			fbxMesh.SetControlPointAt(new FbxVector4(vertices[i].x * scaleFactor,
													  vertices[i].y * scaleFactor,
													  vertices[i].z * scaleFactor,
													  1), i);
		}

		// Set triangles
		int[] triangles = unityMesh.triangles;
		for (int i = 0; i < triangles.Length; i += 3) {
			fbxMesh.BeginPolygon();
			fbxMesh.AddPolygon(triangles[i]);
			fbxMesh.AddPolygon(triangles[i + 1]);
			fbxMesh.AddPolygon(triangles[i + 2]);
			fbxMesh.EndPolygon();
		}

		// Set normals
		Vector3[] normals = unityMesh.normals;
		if (normals.Length > 0) {
			var normalElement = FbxLayerElementNormal.Create(fbxMesh, "Normals");
			normalElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
			normalElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

			var normalArray = normalElement.GetDirectArray();
			for (int i = 0; i < normals.Length; i++) {
				normalArray.Add(new FbxVector4(normals[i].x, normals[i].y, normals[i].z, 0));
			}

			fbxMesh.GetLayer(0).SetNormals(normalElement);
		}

		// Set vertex colors
		Color[] colors = unityMesh.colors;
		if (colors.Length > 0) {
			var colorElement = FbxLayerElementVertexColor.Create(fbxMesh, "Colors");
			colorElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
			colorElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

			var colorArray = colorElement.GetDirectArray();
			for (int i = 0; i < colors.Length; i++) {
				colorArray.Add(new FbxVector4(colors[i].r, colors[i].g, colors[i].b, colors[i].a));
			}

			fbxMesh.GetLayer(0).SetVertexColors(colorElement);
		}

		// Set UV maps
		if (unityMesh.uv.Length > 0) {
			var uvElement = FbxLayerElementUV.Create(fbxMesh, "UVs");
			uvElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
			uvElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

			var uvArray = uvElement.GetDirectArray();
			for (int i = 0; i < unityMesh.uv.Length; i++) {
				uvArray.Add(new FbxVector2(unityMesh.uv[i].x, unityMesh.uv[i].y));
			}

			fbxMesh.GetLayer(0).SetUVs(uvElement);
		}

		return fbxMesh;
	}
}
