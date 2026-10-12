import itertools
INF=10**9
def bf(n,s,E,passes):
    d=[INF]*n; d[s]=0
    for _ in range(passes):
        for (u,v,w) in E:
            if d[u]!=INF and d[u]+w<d[v]: d[v]=d[u]+w
    return d
E=[(0,1,4),(0,2,1),(2,1,1),(1,3,4),(2,3,5),(3,4,1),(1,4,9)]
right=bf(5,0,E,4)
print("正确 =", right)
one=[]; multi=[]
for p in itertools.permutations(E):
    if bf(5,0,p,1)==right: one.append(p)
    else: multi.append(p)
print("一遍就对的排列数 =", len(one), " / 5040")
print("换个顺序就露馅的排列数 =", len(multi))
print()
print("--- 现在数据文件里的顺序（按 边U 数组）---")
cur=[(0,1,4),(0,2,1),(2,1,1),(2,3,5),(3,4,1),(1,3,4),(1,4,9)]
print("  顺序:", [(u,v,w) for u,v,w in cur])
print("  松一遍   =", bf(5,0,cur,1))
print("  松四遍   =", bf(5,0,cur,4))
print("  所以只松一遍**也是对的** ⇒ 输出检查拦不住「只松一遍」")
print()
print("--- 改成这个顺序（只换位置，数字一个不动）---")
new=[(0,1,4),(2,1,1),(2,3,5),(1,4,9),(0,2,1),(1,3,4),(3,4,1)]
print("  顺序:", [(u,v,w) for u,v,w in new])
print("  松一遍   =", bf(5,0,new,1))
print("  松四遍   =", bf(5,0,new,4))
print("  松一遍错在：", [(i,bf(5,0,new,1)[i],right[i]) for i in range(5) if bf(5,0,new,1)[i]!=right[i]])