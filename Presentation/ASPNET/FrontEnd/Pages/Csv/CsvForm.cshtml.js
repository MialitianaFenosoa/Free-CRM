const App = {
    setup() {
        const state = Vue.reactive({
            isSubmitting: false,
            fileInputs: [{ id: Date.now(), file: null }], // Liste des inputs dynamiques
            files: [],
            separator: ",",
            createdById: StorageManager.getUserId()
        });

        const handleFileUpload = (event, inputId) => {
            const selectedFile = event.target.files[0];
            if (selectedFile) {
                const inputIndex = state.fileInputs.findIndex(input => input.id === inputId);
                if (inputIndex !== -1) {
                    state.fileInputs[inputIndex].file = selectedFile;
                }
            }
        };

        const addFileInput = () => {
            state.fileInputs.push({ id: Date.now(), file: null });
        };

        const removeFileInput = (inputId) => {
            state.fileInputs = state.fileInputs.filter(input => input.id !== inputId);
        };

        const handleSubmit = async () => {
            state.files = state.fileInputs.map(input => input.file).filter(file => file !== null);

            if (state.files.length === 0) {
                Swal.fire({
                    icon: "warning",
                    title: "Missing Fields",
                    text: "Please select at least one CSV file.",
                    confirmButtonText: "OK"
                });
                return;
            }

            const formData = new FormData();
            state.files.forEach(file => {
                formData.append("files", file);
            });
            formData.append("separator", state.separator);
            formData.append("createdById", state.createdById);

            try {
                state.isSubmitting = true;

                const response = await AxiosManager.post("/Csv/Import", formData, {
                    headers: { "Content-Type": "multipart/form-data" }
                });

                if (response.data.code === 200) {
                    Swal.fire({
                        icon: "success",
                        title: "Import Successful",
                        text: response.data.content?.message || "Files have been imported successfully.",
                        timer: 2000,
                        showConfirmButton: false
                    });

                    setTimeout(() => window.location.reload(), 2000);
                } else {
                    Swal.fire({
                        icon: "error",
                        title: "Import Failed",
                        text: response.data.message || "An error occurred.",
                        confirmButtonText: "Retry"
                    });
                }
            } catch (error) {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: error.response?.data?.message || "The import process could not be completed.",
                    confirmButtonText: "OK"
                });
            } finally {
                state.isSubmitting = false;
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(["Expenses"]);
                await SecurityManager.validateToken();
            } catch (error) {
                console.error("Security Error:", error);
                Swal.fire({
                    icon: "error",
                    title: "Access Denied",
                    text: "You do not have permission to access this page.",
                    confirmButtonText: "OK"
                });
            }
        });

        return {
            state,
            handleFileUpload,
            addFileInput,
            removeFileInput,
            handleSubmit
        };
    }
};

Vue.createApp(App).mount("#app");
