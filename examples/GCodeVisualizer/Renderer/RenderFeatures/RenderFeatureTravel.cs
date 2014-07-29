﻿/*
Copyright (c) 2014, Lars Brubaker
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
using System.Linq;
using System.Runtime.InteropServices;
using MatterHackers.Agg;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.RenderOpenGl.OpenGl;
using MatterHackers.VectorMath;

namespace MatterHackers.GCodeVisualizer
{
    public class RenderFeatureTravel : RenderFeatureBase
    {
        protected Vector3Float start;
        protected Vector3Float end;
        protected float travelSpeed;

        public RenderFeatureTravel(Vector3 start, Vector3 end, double travelSpeed)
        {
            this.start = new Vector3Float(start);
            this.end = new Vector3Float(end);
            this.travelSpeed = (float)travelSpeed;
        }

        public override void CreateRender3DData(VectorPOD<ColorVertexData> colorVertexData, VectorPOD<int> indexData, Affine transform, double layerScale, RenderType renderType)
        {
            if ((renderType & RenderType.Moves) == RenderType.Moves)
            {
                CreateCylinder(colorVertexData, indexData, new Vector3(start), new Vector3(end), .1, 6, GCodeRenderer.TravelColor, .2);
            }
        }

        public override void Render(Graphics2D graphics2D, Affine transform, double layerScale, RenderType renderType)
        {
            if ((renderType & RenderType.Moves) == RenderType.Moves)
            {
                double movementLineWidth = 0.35 * layerScale;
                RGBA_Bytes movementColor = new RGBA_Bytes(10, 190, 15);

                PathStorage pathStorage = new PathStorage();
                VertexSourceApplyTransform transformedPathStorage = new VertexSourceApplyTransform(pathStorage, transform);
                Stroke stroke = new Stroke(transformedPathStorage, movementLineWidth);

                stroke.line_cap(LineCap.Round);
                stroke.line_join(LineJoin.Round);

                pathStorage.Add(start.x, start.y, ShapePath.FlagsAndCommand.CommandMoveTo);
                if (end.x != start.x || end.y != start.y)
                {
                    pathStorage.Add(end.x, end.y, ShapePath.FlagsAndCommand.CommandLineTo);
                }
                else
                {
                    pathStorage.Add(end.x + .01, end.y, ShapePath.FlagsAndCommand.CommandLineTo);
                }
                graphics2D.Render(stroke, 0, movementColor);
            }
        }
    }
}