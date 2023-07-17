# TLabCurveTool
This is a utility tool for Unity that allows you to place objects in a sequence like Blender's curves.  
[I made it based on this tutorial.](https://www.youtube.com/playlist?list=PLFt_AvWsXl0d8aDaovNztYf6iTChHzrHP)

## Screenshot  
Simple road mesh generation  
<img src="https://github.com/TLabAltoh/TLabCurveTool/assets/121733943/6237fdb5-43b2-4092-98c1-3bf537e49c1a" width="512">

Sequential placement of arbitrary meshes (Textured cubes)  
<img src="https://github.com/TLabAltoh/TLabCurveTool/assets/121733943/0f5e706d-405c-4816-a583-ece6c2393308" width="512">

## Operating Environment
- Unity: 2022.3.3f1  

### Installing
Clone the repository to any directory under Assets in the Unity project that will use the assets with the following command  
```
git clone https://github.com/TLabAltoh/TLabCurveEditor.git
```
If you are adding to an existing git project, use the following command instead
```
git submodule add https://github.com/TLabAltoh/TLabCurveEditor.git
```

### How To Use
Create an arbitrary game object and attach this script to it  
<img src="https://github.com/TLabAltoh/TLabCurveTool/assets/121733943/58c86d2f-a105-4fe7-848d-ed691cea75fb" width="512">
  
```
A: Add segment  
D: Deletion of segment  
S: Insert segment
```

### TODO
- Intersection generation

### Reference
- [Sebastian Lague](https://www.youtube.com/playlist?list=PLFt_AvWsXl0d8aDaovNztYf6iTChHzrHP)
- [Road Texture](https://www.freepik.com/free-photo/lines-traffic-paved-roads-background_3738059.htm)
