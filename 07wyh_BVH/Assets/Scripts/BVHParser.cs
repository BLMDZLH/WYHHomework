using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class BVHParser
{
    public static BVHData Parse(string text)
    {
        var data = new BVHData();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int lineIndex = 0;
        int channelIndex = 0;
        var stack = new Stack<BVHJoint>();
        BVHJoint current = null;

        // ---------- HIERARCHY ----------
        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex].Trim();
            lineIndex++;

            if (line == "HIERARCHY") continue;

            if (line.StartsWith("MOTION")) break;

            if (line.StartsWith("ROOT") || line.StartsWith("JOINT"))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                current = new BVHJoint { name = parts[1] };
                if (stack.Count > 0)
                {
                    current.parent = stack.Peek();
                    stack.Peek().children.Add(current);
                }
                data.joints.Add(current);
            }
            else if (line.StartsWith("End Site"))
            {
                current = new BVHJoint
                {
                    name = "EndSite_" + (stack.Count > 0 ? stack.Peek().name : "root"),
                    parent = stack.Peek()
                };
                stack.Peek().children.Add(current);
                data.joints.Add(current);
            }
            else if (line == "{")
            {
                stack.Push(current);
            }
            else if (line == "}")
            {
                stack.Pop();
                current = stack.Count > 0 ? stack.Peek() : null;
            }
            else if (line.StartsWith("OFFSET"))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                current.offset = new Vector3(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture));
            }
            else if (line.StartsWith("CHANNELS"))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int count = int.Parse(parts[1]);
                for (int i = 0; i < count; i++)
                {
                    current.channels.Add(new BVHChannel
                    {
                        type = parts[2 + i],
                        index = channelIndex++
                    });
                }
            }
        }

        // ---------- MOTION ----------
        if (lineIndex < lines.Length && lines[lineIndex].Trim().StartsWith("Frames:"))
        {
            data.frameCount = int.Parse(lines[lineIndex].Split(':')[1].Trim());
            lineIndex++;
        }
        if (lineIndex < lines.Length && lines[lineIndex].Trim().StartsWith("Frame Time:"))
        {
            data.frameTime = float.Parse(lines[lineIndex].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
            lineIndex++;
        }

        for (int f = 0; f < data.frameCount && lineIndex < lines.Length; f++, lineIndex++)
        {
            var parts = lines[lineIndex].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var frame = new float[channelIndex];
            for (int i = 0; i < channelIndex && i < parts.Length; i++)
                frame[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            data.frames.Add(frame);
        }

        return data;
    }
}