EDGES = [(0,1),(0,2),(1,2),(1,3),(2,3),(4,5),(5,4)]
N = 6
g = [[0]*N for _ in range(N)]
for a,b in EDGES: g[a][b]=1; g[b][a]=1
def dfs(v, seen, out):
    out.append(v); seen[v]=1
    for j in range(N):
        if g[v][j] and not seen[j]: dfs(j,seen,out)
seen=[0]*N; out=[]; dfs(0,seen,out); print("DFS:", out)
seen=[0]*N; out=[]; q=[0]; seen[0]=1
while q:
    v=q.pop(0); out.append(v)
    for j in range(N):
        if g[v][j] and not seen[j]: seen[j]=1; q.append(j)
print("BFS:", out)
seen=[0]*N; blocks=0
for i in range(N):
    if not seen[i]:
        blocks+=1; dfs(i,seen,[])
print("连通块数 =", blocks)