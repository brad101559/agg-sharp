﻿/*
Copyright (c) 2015, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.DataConverters3D;
using MatterHackers.PolygonMesh;
using MatterHackers.PolygonMesh.Csg;
using MatterHackers.PolygonMesh.Processors;

namespace MatterHackers.DataConverters3D
{
	public static class MeshFileIo
	{
		public static IObject3D Load(Stream fileStream, string fileExtension, CancellationToken cancellationToken, Action<double, string> reportProgress = null, IObject3D source = null)
		{
			switch (fileExtension.ToUpper())
			{
				case ".STL":

					var result = source ?? new Object3D();
					result.SetMeshDirect(StlProcessing.Load(fileStream, cancellationToken, reportProgress));
					return result;

				case ".AMF":
					//return AmfProcessing.Load(fileStream, reportProgress);
					return AmfDocument.Load(fileStream, cancellationToken, reportProgress, source);

				case ".OBJ":
					return ObjSupport.Load(fileStream, cancellationToken, reportProgress);

				default:
					return null;
			}
		}

		public static IObject3D Load(string meshPathAndFileName, CancellationToken cancellationToken, Action<double, string> reportProgress = null, IObject3D source = null)
		{
			try
			{
				using (Stream stream = new FileStream(meshPathAndFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					var loadedItem = Load(stream, Path.GetExtension(meshPathAndFileName), cancellationToken, reportProgress, source);
					if (loadedItem != null)
					{
						loadedItem.MeshPath = meshPathAndFileName;
					}

					return loadedItem;
				}
			}
			catch(Exception e)
			{
				Debug.Print(e.Message);
				return null;
			}
		}

		public static async Task<IObject3D> LoadAsync(string meshPathAndFileName, CancellationToken cancellationToken, Action<double, string> reportProgress = null)
		{
			return await Task.Run(() => Load(meshPathAndFileName, cancellationToken, reportProgress));
		}

		public static bool Save(IObject3D context, string meshPathAndFileName, CancellationToken cancellationToken, MeshOutputSettings outputInfo = null, Action<double, string> reportProgress = null)
		{
			// TODO: Seems conceptually correct but needs validation and refinements
			throw new NotImplementedException("Need to re-write this to do what we want, Lars.");
			//var meshGroups = new List<MeshGroup> { context.Flatten() };
			//return Save(meshGroups, meshPathAndFileName, cancellationToken, outputInfo, reportProgress);
		}

		public static bool Save(Mesh mesh, string meshPathAndFileName, CancellationToken cancellationToken, MeshOutputSettings outputInfo = null)
		{
			return Save(new MeshGroup(mesh), meshPathAndFileName, cancellationToken, outputInfo);
		}

		public static bool Save(MeshGroup meshGroupToSave, string meshPathAndFileName, CancellationToken cancellationToken, MeshOutputSettings outputInfo = null)
		{
			List<MeshGroup> meshGroupsToSave = new List<MeshGroup>();
			meshGroupsToSave.Add(meshGroupToSave);
			return Save(meshGroupsToSave, meshPathAndFileName, cancellationToken, outputInfo);
		}

		public static bool Save(List<MeshGroup> meshGroupsToSave, string meshPathAndFileName, CancellationToken cancellationToken, MeshOutputSettings outputInfo = null, Action<double, string> reportProgress = null)
		{
			try
			{
				if (outputInfo == null)
				{
					outputInfo = new MeshOutputSettings();
				}
				switch (Path.GetExtension(meshPathAndFileName).ToUpper())
				{
					case ".STL":
						Mesh mesh = DoMerge(meshGroupsToSave, outputInfo);
						return StlProcessing.Save(mesh, meshPathAndFileName, cancellationToken, outputInfo);

					case ".AMF":
						outputInfo.ReportProgress = reportProgress;
						return AmfDocument.Save(meshGroupsToSave, meshPathAndFileName, outputInfo);

					case ".QBJ":
						outputInfo.ReportProgress = reportProgress;
						return ObjSupport.Save(meshGroupsToSave, meshPathAndFileName, outputInfo);

					default:
						return false;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static Mesh DoMerge(List<MeshGroup> meshGroupsToMerge, MeshOutputSettings outputInfo)
		{
			if(meshGroupsToMerge.Count == 1 
				&& meshGroupsToMerge[0].Meshes.Count == 1)
			{
				return meshGroupsToMerge[0].Meshes[0];
			}
			Mesh allPolygons = new Mesh();
			if (outputInfo.CsgOptionState == MeshOutputSettings.CsgOption.DoCsgMerge)
			{
				foreach (MeshGroup meshGroup in meshGroupsToMerge)
				{
					foreach (Mesh mesh in meshGroup.Meshes)
					{
						allPolygons = CsgOperations.Union(allPolygons, mesh);
					}
				}
			}
			else
			{
				foreach (MeshGroup meshGroup in meshGroupsToMerge)
				{
					foreach (Mesh mesh in meshGroup.Meshes)
					{
						foreach (Face face in mesh.Faces)
						{
							List<IVertex> faceVertices = new List<IVertex>();
							foreach (FaceEdge faceEdgeToAdd in face.FaceEdges())
							{
								// we allow duplicates (the true) to make sure we are not changing the loaded models accuracy.
								IVertex newVertex = allPolygons.CreateVertex(faceEdgeToAdd.FirstVertex.Position, CreateOption.CreateNew, SortOption.WillSortLater);
								faceVertices.Add(newVertex);
							}

							// we allow duplicates (the true) to make sure we are not changing the loaded models accuracy.
							allPolygons.CreateFace(faceVertices.ToArray(), CreateOption.CreateNew);
						}
					}
				}
			}

			return allPolygons;
		}

		public static long GetEstimatedMemoryUse(string fileLocation)
		{
			switch (Path.GetExtension(fileLocation).ToUpper())
			{
				case ".STL":
					return StlProcessing.GetEstimatedMemoryUse(fileLocation);

				case ".AMF":
					return AmfDocument.GetEstimatedMemoryUse(fileLocation);

				case ".OBJ":
					throw new NotImplementedException();
			}

			return 0;
		}

		public static string ComputeSHA1(string destPath)
		{
			// Alternatively: MD5.Create(),  new SHA256Managed()
			var timer = Stopwatch.StartNew();

			using (var stream = new BufferedStream(File.OpenRead(destPath), 1200000))
			{
				byte[] checksum = SHA1.Create().ComputeHash(stream);
				Console.WriteLine("SHA1 computed in {0}ms", timer.ElapsedMilliseconds);
				return BitConverter.ToString(checksum).Replace("-", String.Empty);
			}
		}
	}

	public static class MeshFileIoExtensions
	{
		public static bool Save(this Mesh mesh, string fileName, CancellationToken cancellationToken)
		{
			return MeshFileIo.Save(mesh, fileName, cancellationToken);
		}
	}
}