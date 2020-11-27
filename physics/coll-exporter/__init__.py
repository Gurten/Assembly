# Author: Gurten






bl_info = {
    "name": "Halo 1 CE haloce-coll Exporter for collision",
    "author": "Gurten",
    "version": ( 1, 0, 1 ),
    "blender": ( 2, 80, 0 ),
    "location": "File > Export > Halo 1 CE haloce-coll Exporter (.haloce-coll)",
    "description": "Halo 1 CE COLL Exporter (.haloce-coll)",
    "warning": "",
    "wiki_url": "",
    "tracker_url": "",
    "category": "Import-Export"
}

import bpy, os, subprocess, tempfile
from bpy_extras.io_utils import ExportHelper
from bpy.props import StringProperty, BoolProperty

class CachedMaterials:
    
    def __init__(self, serialized_text, material_dictionary):
        self.serialized_text = serialized_text
        self.material_dictionary = material_dictionary


class Export(bpy.types.Operator, ExportHelper):
    bl_idname = "export.haloce_coll"
    bl_label = "Export"
    __doc__ = "Halo 1 CE COLL Exporter (.haloce-coll)"
    filename_ext = ".haloce-coll"
    filter_glob = StringProperty( default = "*.haloce-coll", options = {'HIDDEN'} )
    
    filepath = StringProperty( 
        name = "File Path",
        description = "File path used for exporting the haloce-coll file",
        maxlen = 1024,
        default = "" )
            
    option_selection_only = BoolProperty(
        name = "Selection Only",
        description = "Exports selected mesh objects only",
        default = True )
    
    def draw( self, context ):
        layout = self.layout
        box = layout.box()
        box.prop( self, 'option_selection_only' )
    
    def serialize_materials(self, objs):
        out = ""
        # Materials
        materials_l = ['default'] # list maintains order thte materials were added.
        materials_d = {'default' : 0}
        for obj in objs:
            for mat in obj.data.materials:
                if mat.name not in materials_d:
                    materials_l.append(mat.name)
                    materials_d[mat.name] = len(materials_d)
        n_materials = len(materials_l)
        out += ("%d\n" % n_materials)
        
        #write materials
        for m in materials_l: # this is ordered by the order of insertion, not possible with a dict.
            material_name = m
            material_texture = "<none>"
            out += ("%s\n%s\n" % (material_name, material_texture))
            
        return CachedMaterials(out, materials_d)
    
    def serialise_model_to_jms(self, context, obj, material_cache):
        """JMS serialisation for the model.
        
        The tool.exe utility works with JMS files.
        """
        #Single origin node for now.
        n_nodes = 1
        
        # header of JMS file. First two lines unknown, third line is the number of nodes.
        out = "8200\n3251\n%d\n" % (n_nodes)
        #write nodes
        for i in range(n_nodes):
            node_name = "default"
            node_next_sibling = -1
            node_first_child = -1
            node_rotation = (0,0,0,1) #i,j,k,w
            node_translation = (0,0,0) #x,y,z
            #flatten list and format to string
            node_vals = [[node_name, node_next_sibling, node_first_child], node_rotation, node_translation] 
            node_vals = [i for l in node_vals for i in l]
            out+= ("%s\n%d\n%d\n%f\t%f\t%f\t%f\n%f\t%f\t%f\n" % tuple(node_vals))
        
        out += material_cache.serialized_text
        
        # Markers
        n_markers = 0
        out += ("%d\n" % n_markers)
        for i in range(n_markers):
            #
            None # format not known
        n_objs = 1
        out += "%d\n" % n_objs
        
        # Objects
 
        obj_name = obj.name
        n_verts = len(obj.data.vertices)
        out += "%s\n%d\n" % (obj_name, n_verts)
        for v in obj.data.vertices:
            node_idx = 0
            # apply transformations to object
            # scale by 100 to counteract H1CE Tool scale by 0.01. 
            vert_pos = obj.matrix_local @ v.co * 100.0
            vert_normal = v.normal
            node1_idx = -1
            node1_weight = 0
            tex_coord = (0,0,0)
            #flatten list and write to string
            vert_vals = [[node_idx], vert_pos, vert_normal, [node1_idx, node1_weight], tex_coord]
            vert_vals = [item for sublist in vert_vals for item in sublist]
            out += ("%d\n%f\t%f\t%f\n%f\t%f\t%f\n%d\n%f\n%f\t%f\t%f\n" % tuple(vert_vals))
        n_faces = 0
        face_str = ""
        for f in obj.data.polygons:
            face_data = []
            face_vert_indices = [obj.data.loops[idx].vertex_index for idx in f.loop_indices]
            face_unknown = 0
            face_material_idx = f.material_index
            if len(obj.data.materials) is not 0: # Occurs when a face has a material assigned
                # get the global material list index
                face_material_idx = material_cache.material_dictionary[obj.data.materials[face_material_idx].name]
            
            for i in range(len(face_vert_indices)-2):
                face_data.append([[face_unknown, face_material_idx], (face_vert_indices[0], face_vert_indices[i+1], face_vert_indices[i+2])]) 
                n_faces += 1
            
            for l in face_data:
                face_vals = [item for sublist in l for item in sublist]
                face_str += ("%d\n%d\n%d\t%d\t%d\n" % tuple(face_vals))
        out += ("%d\n" % n_faces)
        out += face_str
        return out
    
    def generate_tool_tmpdir(self):
        """Generates a temp-directory with the folder layout that tool.exe expects.
        """
        tmp = tempfile.TemporaryDirectory(suffix=None, prefix=None, dir=None)
        os.mkdir(os.path.join(tmp.name, "data"))
        os.mkdir(os.path.join(tmp.name, "tags"))
        return tmp
    
    def detect_export_objects(self, context):
        if self.option_selection_only:
            objs = [o for o in context.selected_objects]
        else:
            objs = [o for o in context.selectable_objects]
        
        objs = list(filter(lambda x: x.type == "MESH", objs))
        if len(objs) == 0:
            raise Exception("No meshes found to export.")
        return objs
    
    def invoke_tool_for_jms(self, tmpdir_path, model_name, instance_name):
        # detect tool.exe in the script install directory
        script_dir = os.path.dirname(os.path.realpath(__file__))
        tool_path = os.path.join(script_dir, "tool.exe")
        commands = [tool_path, "collision-geometry", "%s/%s" % (model_name, instance_name)]
        output = subprocess.check_output(commands, cwd=tmpdir_path)
        return output.decode()
    
    def highlight_errors_of_object(self, context, ob, degenerate_edge_indices):
        # put the troubled object in edit-mode with the edges selected
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_mode(type="EDGE")
        bpy.ops.mesh.select_all(action = 'DESELECT')
        
    
    def validate_export_log(self, context, ob, export_log):
        log_lines = export_log.splitlines()
        if "ERROR failed to import collision model" not in log_lines[-1]:
            return True
        
        self.highlight_errors_of_object(context, ob) 
        
        raise Exception("Object %s could not be exported. See console for details." % ob.name)
    
    def export_one_model_as_jms(self, context, ob, tmpdir_path, model_name, instance_name, material_cache):
        """Exports one object
        
        args:
            ob: the blender object
            tmpdir_path: path to the tmpdir. Expects child-dirs 'data' and 'tags' have already been created.
            model_name: model name - this is a halo data organization concept.
            instance_name: the instance name - this is a halo data organization concept.
            material_cache: the pre-organized material cache among all models
        """
        #Export.filename_ext
        out = self.serialise_model_to_jms(context, ob, material_cache)
        # Write the JMS into a temporary Tool working-directory.
        export_dir = os.path.join(tmpdir_path, "data", model_name, instance_name, "physics")
        os.makedirs(export_dir)
        export_path = os.path.join(export_dir, instance_name )+ ".jms"
        f = open(export_path, "w")
        f.write(out)
        f.close()
        return export_path
    
    def generate_one_model(self, context, ob, tmpdir_path, model_name, instance_name, material_cache):
        self.export_one_model_as_jms(context, ob, tmpdir_path, model_name, instance_name, material_cache)
        output = self.invoke_tool_for_jms(tmpdir_path, model_name, instance_name)
        #check output
        print("Log of exporting %s:" % ob.name)
        print(output)
        if self.validate_export_log(context, ob, output):
            file_src = os.path.join(tmpdir_path, "tags", model_name, instance_name, instance_name )+ ".model_collision_geometry" 
            f_src = open(file_src, 'rb') 
            f_dest = open(os.path.join(os.path.dirname(self.filepath),instance_name+self.filename_ext ), 'wb')
            f_dest.write(f_src.read())
            f_src.close()
            f_dest.close()

    def execute(self, context):
        n_objs_exported = 0
        objs = self.detect_export_objects(context)
        material_cache = self.serialize_materials(objs)
        tmpdir = self.generate_tool_tmpdir()
        fpath = self.filepath
        fname = fpath[(fpath.rfind('\\')+1):].split(self.filename_ext)[0]
        
        if len(objs) > 1:
            for i,obj in enumerate(objs):
                ifname = ("%s%03d" % (fname, i))
                self.generate_one_model(context, obj, tmpdir.name, fname, ifname, material_cache)
                n_objs_exported += 1

            print("Successfully exported %d objects to \'%s\'." % (n_objs_exported, self.filepath))
        else:
            self.generate_one_model(context, objs[0], tmpdir.name, fname, fname, material_cache)
            print("Successfully exported %d objects to \'%s\'." % (1, self.filepath))
        
        return {'FINISHED'}
        

# Blender register plugin 
def register():
    bpy.utils.register_class(Export)

def menu_func( self, context ):
    self.layout.operator( Export.bl_idname, text = "Halo 1 CE COLL Exporter (.haloce-coll)" )

def register():
    bpy.utils.register_class( Export )
    bpy.types.TOPBAR_MT_file_export.append( menu_func )

def unregister():
    bpy.utils.unregister_class( Export )
    bpy.types.TOPBAR_MT_file_export.remove( menu_func )

if __name__ == "__main__":
    register()