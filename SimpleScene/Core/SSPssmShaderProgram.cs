// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene;
using System.Drawing;
using System.Collections.Generic;

namespace SimpleScene
{
    public class SSPssmShaderProgram : SSShaderProgram
    {
        private static string c_ctx = "./Shaders/Shadowmap";

        #region Shaders
        private readonly SSShader m_vertexShader;  
        private readonly SSShader m_fragmentShader;
        private readonly SSShader m_geometryShader;
        #endregion

        #region Uniform Locations
        private readonly int u_numShadowMaps;
        private readonly int u_objectWorldTransform;
        private readonly int[] u_shadowMapVPs = new int[SSParallelSplitShadowMap.c_numberOfSplits];
        #endregion

        #region Uniform Modifiers
        public Matrix4 UniObjectWorldTransform {
            // pass object world transform matrix for use in shadowmap lookup
            set { assertActive(); GL.UniformMatrix4(u_objectWorldTransform, false, ref value); }
        }

        public void UpdateShadowMapVPs(Matrix4[] mvps) {
            // pass update mvp matrices for shadowmap lookup
            for (int s = 0; s < SSParallelSplitShadowMap.c_numberOfSplits; ++s) {
                //GL.UniformMatrix4(u_shadowMapVPs + s, false, ref mvps[s]);
                GL.UniformMatrix4(u_shadowMapVPs [s], false, ref mvps [s]);
            }
        }
        #endregion

        public SSPssmShaderProgram()
        {
            if (GL.GetString(StringName.Extensions).ToLower().Contains("gl_ext_gpu_shader4")) {
                m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, "pssm_vertex.glsl");
                m_vertexShader.LoadShader();
                attach(m_vertexShader);

                m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx, "pssm_fragment.glsl");
                m_fragmentShader.LoadShader();
                attach(m_fragmentShader);

                m_geometryShader = SSAssetManager.GetInstance<SSGeometryShader>(c_ctx, "pssm_geometry.glsl");
                m_geometryShader.LoadShader();
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryVerticesOutExt, 3 * SSParallelSplitShadowMap.c_numberOfSplits);
                attach(m_geometryShader);
            } else {
                throw new NotImplementedException ();
            }
            link();
            Activate();

            // TODO: debug passing things through arrays
            for (int i = 0; i < SSParallelSplitShadowMap.c_numberOfSplits; ++i) {
                var str = "shadowMapVPs" + i;
                u_shadowMapVPs[i] = getUniLoc(str);
            }
            //u_shadowMapVPs = getUniLoc("shadowMapVPs");

            //u_shadowMapSplits = getUniLoc("shadowMapSplits");
            u_objectWorldTransform = getUniLoc("objWorldTransform");
            u_numShadowMaps = getUniLoc("numShadowMaps");

            GL.Uniform1(u_numShadowMaps, SSParallelSplitShadowMap.c_numberOfSplits);

            checkErrors();
        }
    }
}