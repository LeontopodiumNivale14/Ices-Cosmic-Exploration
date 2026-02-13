using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Pictomancy;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.Ui.DebugWindowTabs
{
    internal class Ui_OyzinMap
    {
        public static unsafe void Draw()
        {
            var agentMap = AgentMap.Instance();
            if (agentMap == null)
            {
                ImGui.Text("AgentMap is null!");
                return;
            }

            if (ImGui.CollapsingHeader("Event Markers"))
            {
                DrawEventMarkersTable(GetAllEventMarkers());
            }
        }

        // Event Markers
        public unsafe static List<TempMapMarkerData> GetAllEventMarkers()
        {
            var markers = new List<TempMapMarkerData>();
            var agentMap = AgentMap.Instance();

            if (agentMap == null) return markers;

            // Event markers (what you already have)
            foreach (var marker in agentMap->EventMarkers)
            {
                markers.Add(new TempMapMarkerData
                {
                    Position = marker.Position,
                    IconId = marker.IconId,
                });
            }

            /*
            // Temp markers (player-placed map markers)
            foreach (var marker in agentMap->TempMapMarkers)
            {
                Vector3 pos = new(marker.MapMarker.X, 0, marker.MapMarker.Y);

                markers.Add(new TempMapMarkerData
                {
                    Position = pos,
                    IconId = marker.MapMarker.IconId,
                });
            }
            */

            // Mini map markers
            foreach (var marker in agentMap->MiniMapMarkers)
            {
                Vector3 pos = new(marker.MapMarker.X, 0, marker.MapMarker.Y);
                markers.Add(new TempMapMarkerData
                {
                    Position = pos,
                    IconId = marker.MapMarker.IconId,
                });
            }

            return markers;
        }

        private static void DrawEventMarkersTable(List<TempMapMarkerData> markers)
        {
            if (markers.Count == 0)
            {
                ImGui.Text("No markers found!");
                return;
            }

            if (ImGui.BeginTable("Event Markers", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Position");
                ImGui.TableSetupColumn("Icon ID");
                ImGui.TableSetupColumn("Territory");
                ImGui.TableSetupColumn("SubLevel");
                ImGui.TableHeadersRow();

                foreach (var marker in markers)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"({marker.Position.X:F2}, {marker.Position.Y:F2}, {marker.Position.Z:F2})");

                    ImGui.TableNextColumn();
                    ImGui.Text(marker.IconId.ToString());
                    if (Svc.Texture.TryGetFromGameIcon(marker.IconId, out var texture))
                    {
                        ImGui.SameLine();
                        ImGui.Image(texture.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                    }
                    ImGui.TableNextColumn();
                    if (ImGui.Button("Move to"))
                    {
                        P.Navmesh.PathfindAndMoveTo(marker.Position, false);
                    }

                    if (marker.IconId == 63989)
                    {
                        PictoManager.DrawArrow(marker.Position, 0x80FF0000, 0xFFFFFFFF);
                    }
                }

                ImGui.EndTable();
            }
        }

        public class TempMapMarkerData
        {
            public Vector3 Position { get; set; }
            public uint IconId { get; set; }
        }
    }
}