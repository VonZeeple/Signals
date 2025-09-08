"""
Run this script to automatically generate/update assets.
It also checks that some attributes are set correctly.

"""
import json
import glob
import re
import numpy as np
from copy import deepcopy
import pprint


# Load connector model
with open('assets/signals/base_shapes/wire_anchor.json', 'r', encoding='utf-8-sig') as file:
    con_shape = json.load(file)
main_shape = con_shape['elements'][0]
a1 = main_shape['from']
a2 = main_shape['to']
base_pos = np.array([ (a1[0]+a2[0])/2,a1[1],(a1[2]+a2[2])/2])
main_shape["from"] = [a-b for a,b in zip(a1, base_pos)]
main_shape["to"] = [a-b for a,b in zip(a2, base_pos)]

# add terminal model to base models
for model in ['button_on.json',
              'button_off.json',
              'valve_on.json',
              'valve_off.json',
              'resistor.json']+[f'delay_{i}.json' for i in range(6)]:
    with open('assets/signals/base_shapes/'+model, 'r', encoding='utf-8-sig') as file:
        data = json.load(file)
    connectors = [el for el in data['elements'] if re.match('con[1-9]',el['name'])]
    elements = [el for el in data['elements'] if not re.match('con[1-9]',el['name'])]
    if len(connectors) > 0:
        data['textures'].update(con_shape['textures'])

    for i,el in enumerate(connectors):
        a1 = el['from']
        a2 = el['to']
        base_pos = np.array([ (a1[0]+a2[0])/2,a1[1],(a1[2]+a2[2])/2])
        shape = deepcopy(main_shape)
        shape['name'] += f'_{i}'
        for ch in shape['children']:
            ch['name'] += f'_{i}'
        shape["from"] = [a+b for a,b in zip(shape["from"], base_pos)]
        shape["to"] = [a+b for a,b in zip(shape["to"], base_pos)]
        elements += [shape]
    data['elements'] = elements
    with open('assets/signals/shapes/block/'+model, 'w') as f:
        json.dump(data, f, indent='\t')



# find correct position for wire anchor from model
def get_boxes(path):
    print(path)
    with open(path, 'r', encoding='utf-8-sig') as file:
        data = json.load(file)
        elements = [el for el in data['elements'] if re.match('con[1-9]',el['name'])]
        w = 2
        h = 4
        print("Connector selection box")
        for el in elements:
            print(el['name'])
            a1 = el['from']
            a2 = el['to']
            base_pos = np.array([ (a1[0]+a2[0])/2,a1[1],(a1[2]+a2[2])/2])
            a1 = base_pos + np.array([-w/2, 0, -w/2])
            a2 = base_pos + np.array([w/2, h, w/2])
            a1/=16
            a2/=16
            out=f"""
                    "x1": {a1[0]},
                    "y1": {a1[1]},
                    "z1": {a1[2]},
                    "x2": {a2[0]},
                    "y2": {a2[1]},
                    "z2": {a2[2]},
    """
            print(out)

        print("Base selection box")
        elements = [el for el in data['elements'] if re.match('base[0-9]*',el['name'])]
        a1 = [16,16,16]
        a2 = [0,0,0]
        for el in elements:
            print(el['name'])
            for i in range(3):
                if el['from'][i] < a1[i]:
                    a1[i] = el['from'][i]
            for i in range(3):
                if el['to'][i] > a2[i]:
                    a2[i] = el['to'][i]
        a1 = np.array(a1)/16
        a2 = np.array(a2)/16
        out = f"""
                "x1": {a1[0]},
                "y1": {a1[1]},
                "z1": {a1[2]},
                "x2": {a2[0]},
                "y2": {a2[1]},
                "z2": {a2[2]},
    """
        print("selection box")
        print(out)

        elements = [el for el in data['elements'] if re.search('_col$', el['name'])]
        for el in elements:
            a1 = np.array(el['from'])/16
            a2 = np.array(el['to'])/16
            out = f"""
                "x1": {a1[0]},
                "y1": {a1[1]},
                "z1": {a1[2]},
                "x2": {a2[0]},
                "y2": {a2[1]},
                "z2": {a2[2]},
    """
            print(el['name'])
            print(out)

get_boxes('assets/signals/shapes/block/delay_0.json')

