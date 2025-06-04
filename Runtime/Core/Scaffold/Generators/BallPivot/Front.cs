using System;
using System.Collections.Generic;
using renge_pcl;

class Front
{
    public LinkedList<Edge> front;
    LinkedListNode<Edge> pos;
    SortedDictionary<int, Dictionary<Edge, LinkedListNode<Edge>>> points;

    public Front()
    {
        points = new SortedDictionary<int, Dictionary<Edge, LinkedListNode<Edge>>>();
        front = new LinkedList<Edge>();
        pos = front.First;
    }

    internal Edge GetActiveEdge()
    {
        Edge e = null;
        if (front != null && front.Count > 0)
        {
            bool firstLoop = true;
            for (LinkedListNode<Edge> it = pos; ; it = it.Next)
            {
                if (it == null)
                {
                    it = front.First;
                }
                if (!firstLoop && it == pos)
                {
                    break;
                }
                if (it.Value.Active)
                {
                    pos = it;
                    e = it.Value;
                    break;
                }
            }
        }

        return e;
    }

    internal void AddEdges(Triangle tri)
    {
        for (int i = 0; i < 3; i++)
        {
            front.AddLast(tri.GetEdge(i));
            AddEdgePoints(front.Last);
        }
    }

    internal bool InFront(int index)
    {
        return points.ContainsKey(index);
    }

    internal void JoinAndGlue(Tuple<int, Triangle> tri, Pivoter pivoter)
    {
        //join and glue prototype
        //if (f.Contains(new Edge(e.First, p)))
        //	Glue(new Edge(p, e.First), new Edge(e.First, p));
        //if (f.Contains(new Edge(e.Second, p)))
        //	Glue(new Edge(p, e.First), new Edge(p, e.First));

        if (!pivoter.IsUsed(tri.Item1))
        {
            for (int i = 0; i < 2; i++)
            {
                Edge e = tri.Item2.GetEdge(i);
                LinkedListNode<Edge> insertionPlace = front.AddBefore(pos, e);
                AddEdgePoints(insertionPlace);
            }

            RemoveEdgePoints(pos.Value);

            bool atEnd = false;
            var tmp = pos.Next;
            if (tmp == null)
            {
                tmp = pos.Previous;
                atEnd = true;
            }
            front.Remove(pos);
            //move iterator to first added edge
            if (!atEnd)
                pos = tmp.Previous.Previous;
            else
                pos = tmp.Previous;


            pivoter.SetUsed(tri.Item1);
        }
        else if (InFront(tri.Item1))
        {
            int added = 0;
            for (int i = 0; i < 2; i++)
            {
                Edge e = tri.Item2.GetEdge(i);
                LinkedListNode<Edge> it = IsPresent(e);
                if (it != null)
                {
                    RemoveEdgePoints(it.Value);
                    front.Remove(it);
                }
                else
                {
                    LinkedListNode<Edge> insertionPlace = front.AddBefore(pos, e);
                    AddEdgePoints(insertionPlace);
                    added--;
                }
            }

            var tmp = pos.Next;
            if (tmp == null)
            {
                tmp = pos.Previous;
                added++;
            }
            RemoveEdgePoints(pos.Value);
            front.Remove(pos);
            pos = tmp;

            if (added < 0)
            {
                while (added < 0)
                {
                    pos = pos.Previous;
                    added++;
                }
            }
            else
            {
                pos = front.First;
            }


        }
        else
        {
            SetInactive(pos.Value);
        }
    }

    internal void SetInactive(Edge e)
    {
        e.Active = false;
        RemoveEdgePoints(e);
        if (front.First == pos)
        {
            front.Remove(pos);
            pos = front.First;
        }
        else
        {
            var tmp = pos.Previous;
            front.Remove(pos);
            pos = tmp;
        }
    }

    private LinkedListNode<Edge> IsPresent(Edge e)
    {
        int vertex0 = e.First.Item2;
        int vertex1 = e.Second.Item2;

        if (!points.ContainsKey(vertex0) || !points.ContainsKey(vertex1))
        {
            return null;
        }
        else
        {
            foreach (var pair in points[vertex0])
            {
                int v0 = pair.Value.Value.First.Item2;
                int v1 = pair.Value.Value.Second.Item2;
                if ((v0 == vertex1 && v1 == vertex0) || (v0 == vertex0 && v1 == vertex1))
                {
                    return pair.Value;
                }
            }
        }

        return null;
    }

    private void AddEdgePoints(LinkedListNode<Edge> edge)
    {
        //add first vertex
        Tuple<PointNormal, int> data = edge.Value.First;
        if (!points.ContainsKey(data.Item2))
        {
            points[data.Item2] = new Dictionary<Edge, LinkedListNode<Edge>>();
        }
        points[data.Item2][edge.Value] = edge;

        //add second vertex
        data = edge.Value.Second;
        if (!points.ContainsKey(data.Item2))
        {
            points[data.Item2] = new Dictionary<Edge, LinkedListNode<Edge>>();
        }
        points[data.Item2][edge.Value] = edge;
    }

    private void RemoveEdgePoints(Edge edge)
    {
        //remove first vertex
        Tuple<PointNormal, int> data = edge.First;
        if (points.ContainsKey(data.Item2))
        {
            points[data.Item2].Remove(edge);

            if (points[data.Item2].Count == 0)
            {
                points.Remove(data.Item2);
            }
        }

        //remove second vertex
        data = edge.Second;
        if (points.ContainsKey(data.Item2))
        {
            points[data.Item2].Remove(edge);

            if (points[data.Item2].Count == 0)
            {
                points.Remove(data.Item2);
            }
        }
    }
}