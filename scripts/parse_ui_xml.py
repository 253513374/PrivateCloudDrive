#!/usr/bin/env python3
"""Parse UI hierarchy XML from stdin and print visible text fields."""
import sys
import xml.dom.minidom as md

xml = sys.stdin.read()
dom = md.parseString(xml)

def walk(node, depth=0):
    t = node.getAttribute('text')
    c = node.getAttribute('content-desc')
    h = node.getAttribute('hint')
    cl = node.getAttribute('class')
    b = node.getAttribute('bounds')
    pwd = node.getAttribute('password')
    foc = node.getAttribute('focused')
    clk = node.getAttribute('clickable')
    
    if t and t.strip():
        prefix = "[PWD]" if pwd == "true" else ""
        print(f"{prefix}text={t!r} class={cl} bounds={b} focused={foc} clickable={clk}")
    if c and c.strip():
        print(f"  content-desc={c!r}")
    if h and h.strip():
        print(f"  hint={h!r}")
    
    for child in node.childNodes:
        if child.nodeType == child.ELEMENT_NODE:
            walk(child, depth+1)

for n in dom.documentElement.childNodes:
    if n.nodeType == n.ELEMENT_NODE:
        walk(n)
