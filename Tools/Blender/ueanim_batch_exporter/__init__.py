"""Export UEFormat models and animations as Unity-ready FBX files."""

import math
import os
from pathlib import Path

import bpy
from bpy.props import BoolProperty, CollectionProperty, FloatProperty, PointerProperty, StringProperty
from bpy.types import Context, Object, Operator, OperatorFileListElement, Panel, PropertyGroup


bl_info = {
    "name": "UEFormat Unity Exporter",
    "author": "OpenAI",
    "version": (1, 2, 0),
    "blender": (4, 2, 0),
    "location": "View3D > Sidebar > UEAnim Export",
    "description": "Export UEFormat models and animations as Unity-ready FBX files",
    "category": "Import-Export",
}


def _armature_poll(_self: object, obj: Object) -> bool:
    return obj.type == "ARMATURE"


def _active_armature(context: Context) -> Object | None:
    obj = context.active_object
    if obj is None:
        return None
    if obj.type == "ARMATURE":
        return obj
    if obj.type == "MESH":
        for modifier in obj.modifiers:
            if modifier.type == "ARMATURE" and modifier.object is not None:
                return modifier.object
    return None


class UEAB_Settings(PropertyGroup):
    target_armature: PointerProperty(
        name="Target Armature",
        description="Armature whose bone hierarchy matches the selected UEAnim files",
        type=Object,
        poll=_armature_poll,
    )
    output_directory: StringProperty(
        name="Output Folder",
        description="Folder for the exported FBX files",
        subtype="DIR_PATH",
    )
    model_filename: StringProperty(
        name="Model File Name",
        description="FBX file name for the target armature and its bound meshes; leave empty to use the armature name",
        default="",
    )
    scale_factor: FloatProperty(
        name="UE Import Scale",
        description="Scale passed to UEFormat while importing each animation",
        default=0.01,
        min=0.0001,
        soft_max=1.0,
        precision=4,
    )
    simplify_factor: FloatProperty(
        name="FBX Simplify",
        description="FBX animation curve simplification; use zero to preserve all baked keys",
        default=0.0,
        min=0.0,
        soft_max=10.0,
    )
    bake_visual_keys: BoolProperty(
        name="Bake Evaluated Pose",
        description="Bake the evaluated armature pose before FBX export to match Unity FBX Batch Re-export",
        default=True,
    )
    overwrite: BoolProperty(
        name="Overwrite Existing FBX",
        default=True,
    )


def _supported_fbx_options(options: dict) -> dict:
    operator = bpy.ops.export_scene.fbx
    supported = {prop.identifier for prop in operator.get_rna_type().properties}
    return {name: value for name, value in options.items() if name in supported}


def _unity_fbx_options(output_path: Path, object_types: set[str]) -> dict:
    return {
        "filepath": str(output_path),
        "check_existing": False,
        "use_selection": True,
        "use_visible": False,
        "use_active_collection": False,
        "collection": "",
        "global_scale": 1.0,
        "apply_unit_scale": True,
        "apply_scale_options": "FBX_SCALE_ALL",
        "use_space_transform": False,
        "bake_space_transform": True,
        "object_types": object_types,
        "use_custom_props": False,
        "add_leaf_bones": False,
        "primary_bone_axis": "Y",
        "secondary_bone_axis": "X",
        "use_armature_deform_only": False,
        "armature_nodetype": "NULL",
        "axis_forward": "-Y",
        "axis_up": "Z",
        "path_mode": "COPY",
        "embed_textures": False,
    }


def _is_descendant_of(obj: Object, ancestor: Object) -> bool:
    parent = obj.parent
    while parent is not None:
        if parent == ancestor:
            return True
        parent = parent.parent
    return False


def _bound_meshes(context: Context, armature: Object) -> list[Object]:
    meshes = []
    for obj in context.scene.objects:
        if obj.type != "MESH":
            continue
        uses_armature = any(
            modifier.type == "ARMATURE" and modifier.object == armature
            for modifier in obj.modifiers
        )
        if uses_armature or _is_descendant_of(obj, armature):
            meshes.append(obj)
    return sorted(meshes, key=lambda obj: obj.name.casefold())


def _export_model_fbx(output_path: Path, armature: Object, meshes: list[Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in [armature, *meshes]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature

    options = _unity_fbx_options(output_path, {"ARMATURE", "MESH"})
    options.update({
        "use_mesh_modifiers": True,
        "use_mesh_modifiers_render": True,
        "mesh_smooth_type": "OFF",
        "colors_type": "SRGB",
        "prioritize_active_color": False,
        "use_subsurf": False,
        "use_mesh_edges": False,
        "use_tspace": False,
        "use_triangles": False,
        "bake_anim": False,
    })
    result = bpy.ops.export_scene.fbx(**_supported_fbx_options(options))
    if "FINISHED" not in result:
        raise RuntimeError(f"FBX exporter returned {sorted(result)}")


def _bake_current_animation_to_keys(armature: Object) -> None:
    scene = bpy.context.scene
    selected_bones = {bone.name: bone.select for bone in armature.pose.bones}

    try:
        bpy.ops.object.mode_set(mode="POSE")
        for bone in armature.pose.bones:
            bone.select = True

        pose_result = bpy.ops.nla.bake(
            frame_start=scene.frame_start,
            frame_end=scene.frame_end,
            step=1,
            only_selected=True,
            visual_keying=True,
            clear_constraints=False,
            clear_parents=False,
            use_current_action=True,
            clean_curves=False,
            bake_types={"POSE"},
        )
        if "FINISHED" not in pose_result:
            raise RuntimeError(f"Pose key bake returned {sorted(pose_result)}")

        bpy.ops.object.mode_set(mode="OBJECT")
        object_result = bpy.ops.nla.bake(
            frame_start=scene.frame_start,
            frame_end=scene.frame_end,
            step=1,
            only_selected=True,
            visual_keying=True,
            clear_constraints=False,
            clear_parents=False,
            use_current_action=True,
            clean_curves=False,
            bake_types={"OBJECT"},
        )
        if "FINISHED" not in object_result:
            raise RuntimeError(f"Object key bake returned {sorted(object_result)}")
    finally:
        if armature.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        for bone in armature.pose.bones:
            bone.select = selected_bones.get(bone.name, False)


def _export_animation_fbx(
    output_path: Path,
    armature: Object,
    simplify_factor: float,
    bake_visual_keys: bool,
) -> None:
    action = armature.animation_data.action if armature.animation_data else None
    if action is None:
        raise RuntimeError("UEFormat did not assign an Action to the target armature")

    frame_start, frame_end = action.frame_range
    scene = bpy.context.scene
    scene.frame_start = math.floor(frame_start)
    scene.frame_end = math.ceil(frame_end)
    scene.frame_set(scene.frame_start)

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature

    if bake_visual_keys:
        _bake_current_animation_to_keys(armature)

    options = _unity_fbx_options(output_path, {"ARMATURE"})
    options.update({
        "bake_anim": True,
        "bake_anim_use_all_bones": True,
        "bake_anim_use_nla_strips": False,
        "bake_anim_use_all_actions": False,
        "bake_anim_force_startend_keying": True,
        "bake_anim_step": 1.0,
        "bake_anim_simplify_factor": simplify_factor,
    })
    result = bpy.ops.export_scene.fbx(**_supported_fbx_options(options))
    if "FINISHED" not in result:
        raise RuntimeError(f"FBX exporter returned {sorted(result)}")


def _remove_actions_created_after(action_pointers: set[int]) -> None:
    for action in list(bpy.data.actions):
        if action.as_pointer() not in action_pointers:
            bpy.data.actions.remove(action, do_unlink=True)


class UEAB_OT_SelectAndExport(Operator):
    bl_idname = "ueab.select_and_export"
    bl_label = "Select UEAnim Files and Export"
    bl_description = "Select one or more UEAnim files and export each as a separate FBX"
    bl_options = {"REGISTER"}

    files: CollectionProperty(
        type=OperatorFileListElement,
        options={"HIDDEN", "SKIP_SAVE"},
    )
    directory: StringProperty(subtype="DIR_PATH")
    filter_glob: StringProperty(default="*.ueanim", options={"HIDDEN"})

    def invoke(self, context: Context, _event: object) -> set[str]:
        context.window_manager.fileselect_add(self)
        return {"RUNNING_MODAL"}

    def execute(self, context: Context) -> set[str]:
        settings = context.scene.ueab_settings
        armature = settings.target_armature or _active_armature(context)

        if armature is None or armature.type != "ARMATURE":
            self.report({"ERROR"}, "Choose a target Armature before exporting")
            return {"CANCELLED"}
        if armature.name not in context.view_layer.objects:
            self.report({"ERROR"}, "The target Armature is not in the current View Layer")
            return {"CANCELLED"}
        if not hasattr(context.scene, "uf_settings"):
            self.report({"ERROR"}, "Enable the UEFormat add-on before using this exporter")
            return {"CANCELLED"}
        if not self.files:
            self.report({"ERROR"}, "No UEAnim files were selected")
            return {"CANCELLED"}

        output_setting = settings.output_directory.strip()
        if not output_setting:
            self.report({"ERROR"}, "Choose an output folder before exporting")
            return {"CANCELLED"}
        output_text = bpy.path.abspath(output_setting)

        output_directory = Path(output_text)
        try:
            output_directory.mkdir(parents=True, exist_ok=True)
        except OSError as exc:
            self.report({"ERROR"}, f"Cannot create output folder: {exc}")
            return {"CANCELLED"}

        source_directory = Path(bpy.path.abspath(self.directory))
        sources = sorted(
            (source_directory / file.name for file in self.files),
            key=lambda path: path.name.casefold(),
        )
        missing = [path.name for path in sources if not path.is_file()]
        if missing:
            self.report({"ERROR"}, f"Input file not found: {missing[0]}")
            return {"CANCELLED"}

        selected_before = list(context.selected_objects)
        active_before = context.view_layer.objects.active
        mode_before = active_before.mode if active_before is not None else "OBJECT"
        scene = context.scene
        frame_start_before = scene.frame_start
        frame_end_before = scene.frame_end
        frame_current_before = scene.frame_current
        uf_settings = scene.uf_settings
        ueformat_before = {
            "scale_factor": uf_settings.scale_factor,
            "rotation_only": uf_settings.rotation_only,
            "import_curves": uf_settings.import_curves,
        }
        original_action = armature.animation_data.action if armature.animation_data else None
        original_slot = None
        if armature.animation_data and hasattr(armature.animation_data, "action_slot"):
            original_slot = armature.animation_data.action_slot

        exported = 0
        skipped = 0
        failures: list[str] = []
        context.window_manager.progress_begin(0, len(sources))

        try:
            if active_before is not None and active_before.mode != "OBJECT":
                bpy.ops.object.mode_set(mode="OBJECT")

            bpy.ops.object.select_all(action="DESELECT")
            armature.select_set(True)
            context.view_layer.objects.active = armature

            uf_settings.scale_factor = settings.scale_factor
            uf_settings.rotation_only = False
            uf_settings.import_curves = False

            for index, source_path in enumerate(sources):
                context.window_manager.progress_update(index)
                output_path = output_directory / f"{source_path.stem}.fbx"
                if output_path.exists() and not settings.overwrite:
                    skipped += 1
                    continue

                action_pointers = {action.as_pointer() for action in bpy.data.actions}
                try:
                    result = bpy.ops.uf.import_ueanim(
                        "EXEC_DEFAULT",
                        filepath=str(source_path),
                        directory=str(source_path.parent) + os.sep,
                        files=[{"name": source_path.name}],
                    )
                    if "FINISHED" not in result:
                        raise RuntimeError(f"UEFormat importer returned {sorted(result)}")

                    _export_animation_fbx(
                        output_path,
                        armature,
                        settings.simplify_factor,
                        settings.bake_visual_keys,
                    )
                    exported += 1
                except Exception as exc:  # Blender operators expose heterogeneous exceptions.
                    failures.append(f"{source_path.name}: {exc}")
                    print(f"[UEAnim Batch Exporter] Failed: {source_path} -> {exc}")
                finally:
                    if armature.animation_data:
                        armature.animation_data.action = None
                    _remove_actions_created_after(action_pointers)
        finally:
            context.window_manager.progress_update(len(sources))
            context.window_manager.progress_end()
            uf_settings.scale_factor = ueformat_before["scale_factor"]
            uf_settings.rotation_only = ueformat_before["rotation_only"]
            uf_settings.import_curves = ueformat_before["import_curves"]

            armature.animation_data_create()
            armature.animation_data.action = original_action
            if original_slot is not None and hasattr(armature.animation_data, "action_slot"):
                try:
                    armature.animation_data.action_slot = original_slot
                except RuntimeError:
                    pass

            scene.frame_start = frame_start_before
            scene.frame_end = frame_end_before
            scene.frame_set(frame_current_before)

            bpy.ops.object.select_all(action="DESELECT")
            for obj in selected_before:
                if obj.name in bpy.context.view_layer.objects:
                    obj.select_set(True)
            if active_before is not None and active_before.name in bpy.context.view_layer.objects:
                context.view_layer.objects.active = active_before
                if mode_before != "OBJECT":
                    try:
                        bpy.ops.object.mode_set(mode=mode_before)
                    except RuntimeError:
                        pass

        summary = f"Exported {exported}, skipped {skipped}, failed {len(failures)}"
        if failures:
            self.report({"WARNING"}, summary + "; details are in the Blender console")
        else:
            self.report({"INFO"}, summary)
        return {"FINISHED"}


class UEAB_OT_ExportModel(Operator):
    bl_idname = "ueab.export_model"
    bl_label = "Export Current Model FBX"
    bl_description = "Export the target armature and all bound meshes as a Unity-ready FBX"
    bl_options = {"REGISTER"}

    def execute(self, context: Context) -> set[str]:
        settings = context.scene.ueab_settings
        armature = settings.target_armature or _active_armature(context)
        if armature is None or armature.type != "ARMATURE":
            self.report({"ERROR"}, "Choose a target Armature before exporting")
            return {"CANCELLED"}
        if armature.name not in context.view_layer.objects:
            self.report({"ERROR"}, "The target Armature is not in the current View Layer")
            return {"CANCELLED"}

        meshes = _bound_meshes(context, armature)
        if not meshes:
            self.report({"ERROR"}, "No meshes are bound or parented to the target Armature")
            return {"CANCELLED"}
        unavailable = [obj.name for obj in meshes if obj.name not in context.view_layer.objects]
        if unavailable:
            self.report({"ERROR"}, f"Bound mesh is not in the current View Layer: {unavailable[0]}")
            return {"CANCELLED"}

        output_setting = settings.output_directory.strip()
        if not output_setting:
            self.report({"ERROR"}, "Choose an output folder before exporting")
            return {"CANCELLED"}
        output_directory = Path(bpy.path.abspath(output_setting))
        try:
            output_directory.mkdir(parents=True, exist_ok=True)
        except OSError as exc:
            self.report({"ERROR"}, f"Cannot create output folder: {exc}")
            return {"CANCELLED"}

        filename = Path(settings.model_filename.strip()).name or armature.name
        if not filename.lower().endswith(".fbx"):
            filename += ".fbx"
        output_path = output_directory / filename
        if output_path.exists() and not settings.overwrite:
            self.report({"WARNING"}, f"FBX already exists: {output_path.name}")
            return {"CANCELLED"}

        selected_before = list(context.selected_objects)
        active_before = context.view_layer.objects.active
        mode_before = active_before.mode if active_before is not None else "OBJECT"
        pose_position_before = armature.data.pose_position
        animation_data = armature.animation_data
        original_action = animation_data.action if animation_data else None
        original_use_nla = animation_data.use_nla if animation_data else True
        original_slot = None
        if animation_data and hasattr(animation_data, "action_slot"):
            original_slot = animation_data.action_slot

        try:
            if active_before is not None and active_before.mode != "OBJECT":
                bpy.ops.object.mode_set(mode="OBJECT")

            if animation_data:
                animation_data.action = None
                animation_data.use_nla = False
            armature.data.pose_position = "REST"
            context.view_layer.update()

            _export_model_fbx(output_path, armature, meshes)
        except Exception as exc:
            self.report({"ERROR"}, f"Model export failed: {exc}")
            return {"CANCELLED"}
        finally:
            armature.data.pose_position = pose_position_before
            if animation_data:
                animation_data.use_nla = original_use_nla
                animation_data.action = original_action
                if original_slot is not None and hasattr(animation_data, "action_slot"):
                    try:
                        animation_data.action_slot = original_slot
                    except RuntimeError:
                        pass
            context.view_layer.update()

            bpy.ops.object.select_all(action="DESELECT")
            for obj in selected_before:
                if obj.name in context.view_layer.objects:
                    obj.select_set(True)
            if active_before is not None and active_before.name in context.view_layer.objects:
                context.view_layer.objects.active = active_before
                if mode_before != "OBJECT":
                    try:
                        bpy.ops.object.mode_set(mode=mode_before)
                    except RuntimeError:
                        pass

        self.report({"INFO"}, f"Exported model: {output_path.name}")
        return {"FINISHED"}


class UEAB_PT_BatchExporter(Panel):
    bl_label = "UEFormat Unity Exporter"
    bl_idname = "UEAB_PT_batch_exporter"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "UEAnim Export"

    def draw(self, context: Context) -> None:
        layout = self.layout
        settings = context.scene.ueab_settings

        layout.prop(settings, "target_armature")
        layout.prop(settings, "output_directory")

        layout.separator()
        layout.label(text="Model")
        layout.prop(settings, "model_filename")
        layout.operator(UEAB_OT_ExportModel.bl_idname, icon="MESH_DATA")

        layout.separator()
        layout.label(text="Animations")
        layout.prop(settings, "scale_factor")
        layout.prop(settings, "simplify_factor")
        layout.prop(settings, "bake_visual_keys")
        layout.prop(settings, "overwrite")

        if not hasattr(context.scene, "uf_settings"):
            warning = layout.box()
            warning.label(text="UEFormat add-on is not enabled", icon="ERROR")

        layout.separator()
        layout.operator(UEAB_OT_SelectAndExport.bl_idname, icon="EXPORT")


CLASSES = (
    UEAB_Settings,
    UEAB_OT_SelectAndExport,
    UEAB_OT_ExportModel,
    UEAB_PT_BatchExporter,
)


def register() -> None:
    for cls in CLASSES:
        bpy.utils.register_class(cls)
    bpy.types.Scene.ueab_settings = PointerProperty(type=UEAB_Settings)


def unregister() -> None:
    del bpy.types.Scene.ueab_settings
    for cls in reversed(CLASSES):
        bpy.utils.unregister_class(cls)


if __name__ == "__main__":
    register()
