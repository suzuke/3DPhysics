using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using JigLibX.Geometry;
using JigLibX.Physics;
using JigLibX.Collision;

namespace Winform_XNA.PhysicObjects
{
    class TriangleMeshObject : Gobject
    {
        TriangleMesh triangleMesh;

        public TriangleMeshObject(Model model, Matrix orientation, Vector3 position)
            : base()
        {
            Body = new Body();
            Skin = new CollisionSkin(null);

            triangleMesh = new TriangleMesh();

            List<Vector3> vertexList = new List<Vector3>();
            List<TriangleVertexIndices> indexList = new List<TriangleVertexIndices>();

            ExtractData(vertexList, indexList, model);

            triangleMesh.CreateMesh(vertexList,indexList, 4, 1.0f);
            Skin.AddPrimitive(triangleMesh, new MaterialProperties(0.8f, 0.7f, 0.6f));
            PhysicsSystem.CurrentPhysicsSystem.CollisionSystem.AddCollisionSkin(Skin);

            // Transform
            Skin.ApplyLocalTransform(new JigLibX.Math.Transform(position, orientation));
            // we also need to move this dummy, so the object is *rendered* at the correct positiob
            Body.MoveTo(position, orientation);
            CommonInit(position, new Vector3(1, 1, 1), model, false);
        }


        /// <summary>
        /// Helper Method to get the vertex and index List from the model.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        /// <param name="model"></param>
        public void ExtractData(List<Vector3> vertices, List<TriangleVertexIndices> indices,Model model)
        {
            Matrix[] bones_ = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(bones_);
            foreach (ModelMesh mm in model.Meshes)
            {
                int offset = vertices.Count;
                Matrix xform = bones_[mm.ParentBone.Index];
                foreach (ModelMeshPart mmp in mm.MeshParts)
                {
                    Vector3[] a = new Vector3[mmp.NumVertices];
                    int stride = mmp.VertexBuffer.VertexDeclaration.VertexStride;
                    //mm.VertexBuffer.GetData<Vector3>(mmp.StreamOffset + mmp.BaseVertex * mmp.VertexStride, a, 0, mmp.NumVertices, mmp.VertexStride);  //XNA 4.0 change
                    mmp.VertexBuffer.GetData( mmp.VertexOffset * stride, a, 0, mmp.NumVertices, stride  );

                    for (int i = 0; i != a.Length; ++i)
                        Vector3.Transform(ref a[i], ref xform, out a[i]);
                    vertices.AddRange(a);

                    //if (mm.IndexBuffer.IndexElementSize != IndexElementSize.SixteenBits)      //XNA 4.0 change
                    if (mmp.IndexBuffer.IndexElementSize != IndexElementSize.SixteenBits)
                        throw new Exception( String.Format( "Model uses 32-bit indices, which are not supported." ) );

                    short[] s = new short[mmp.PrimitiveCount * 3];
                    //mm.IndexBuffer.GetData<short>(mmp.StartIndex * 2, s, 0, mmp.PrimitiveCount * 3);      //XNA 4.0 change
                    mmp.IndexBuffer.GetData( mmp.StartIndex * 2, s, 0, mmp.PrimitiveCount * 3 );

                    JigLibX.Geometry.TriangleVertexIndices[] tvi = new JigLibX.Geometry.TriangleVertexIndices[mmp.PrimitiveCount];
                    for (int i = 0; i != tvi.Length; ++i)
                    {
                        tvi[i].I0 = s[i * 3 + 2] + offset;
                        tvi[i].I1 = s[i * 3 + 1] + offset;
                        tvi[i].I2 = s[i * 3 + 0] + offset;
                    }
                    indices.AddRange(tvi);
                }
            }
        }
    }
}
