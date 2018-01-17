window.onload = function() {
  // Event bus object
  var bus = new Vue();

  Vue.component('rpa-node-instance', {
    template: '#tmpl-node-instance',

    props: ['id', 'type', 'displayType', 'name', 'prop'],

    data: function() {
      return {
        isSelected: false,
        isDragged: false,
        isSuccessorOn: false,
      };
    },

    computed: {
      canHaveSuccessor: function() {
        return this.type !== 'End';
      },

      draggable: function() {
        if (this.type === 'Start' || this.type === 'End') {
          return 'false';
        } else {
          return 'true';
        }
      },
    },

    created: function() {
      var self = this;
      bus.$on('workflow-canvas.selectnode', function(id) {
        if (self.id === id) {
          self.isSelected = true;
          bus.$emit('node-instance.select', self);
        } else {
          self.isSelected = false;
        }
      });

      bus.$on('workflow-canvas.getselectednode', function(callback) {
        if (self.isSelected) {
          callback(self);
        }
      });
    },

    updated: function() {
      this.$emit('nodepropertychange', {
        id: this.id,
        name: this.name,
        prop: this.prop,
      });
    },

    methods: {
      onClick: function(e) {
        bus.$emit('node-instance.click', this);
      },

      onDragStart: function(e) {
        this.isDragged = true;

        // Set dragged node class info
        e.dataTransfer.setData('text', JSON.stringify({
          id: this.id,
          type: this.type,
          displayType: this.displayType,
          name: this.name,
          prop: this.prop,
        }));
      },

      onDragEnd: function() {
        this.isDragged = false;
      },

      onSuccessorDragEnter: function() {
        this.isSuccessorOn = true;
      },

      onSuccessorDragOver: function(e) {
        if (e.preventDefault) {
          e.preventDefault();
        }
        return false;
      },

      onSuccessorDragLeave: function() {
        this.isSuccessorOn = false;
      },

      onSuccessorDrop: function(e) {
        if (e.stopPropagation) {
          e.stopPropagation();
        }
        if (e.preventDefault) {
          e.preventDefault();
        }

        this.isSuccessorOn = false;

        // Get dropped node info
        var nodeInfo = JSON.parse(e.dataTransfer.getData('text'));

        // Notify to parent
        var action = nodeInfo.id ? 'move' : 'create';
        this.$emit('successordrop', this.id, action, nodeInfo);

        return false;
      },
    },
  });

  Vue.component('rpa-node-class', {
    template: '#tmpl-node-class',

    props: ['type', 'displayType'],

    methods: {
      onDragStart: function(e) {
        // Set dragged node class info
        e.dataTransfer.setData('text', JSON.stringify({
          type: this.type,
          displayType: this.displayType,
        }));
      },
    },
  });

  Vue.component('rpa-node-palette', {
    template: '#tmpl-node-palette',

    data: function() {
      return {
        // Node classes for making workflow
        nodeClasses: [
          {type: 'ImageMatching', displayType: 'Image Matching'},
          {type: 'KeyboardInput', displayType: 'Keyboard Input'},
          {type: 'OpenExplorer', displayType: 'Open Folder'},
          {type: 'FileRead', displayType: 'File Read'},
          {type: 'Wait', displayType: 'Wait'},
        ],
      };
    },
  });

  Vue.component('rpa-workflow-canvas', {
    template: '#tmpl-workflow-canvas',

    data: function() {
      return {
        id: "",
        name: "New Workflow",
        workflow: [
          {id: uuid(), type: 'Start', displayType: 'Start', name: 'Start'},
          {id: uuid(), type: 'End', displayType: 'End', name: 'End'},
        ],

        isSaving: false,
      };
    },

    created: function() {
      var self = this;

      window.addEventListener('keydown', function(e) {
        var DELETE = 46
        if (e.keyCode === DELETE) {
          // Delete selected node
          bus.$emit('workflow-canvas.getselectednode', function(nodeInstance) {
            var i = self.findPositionById(nodeInstance.id);
            if (i !== -1) {
              self.workflow.splice(i, 1);
            }
          });
        }
      });

      bus.$on('node-instance.click', function(nodeInstance) {
        bus.$emit('workflow-canvas.selectnode', nodeInstance.id);
      });

      bus.$on('workflow-list.selectworkflow', function(workflow) {
        self.loadWorkflow(workflow);
      });
    },

    methods: {
      loadWorkflow: function(workflow) {
        this.id = workflow.id;
        this.name = workflow.name;
        this.workflow = workflow.data;
      },

      findPositionById: function(id) {
        for (var i = 0; i < this.workflow.length; i++) {
          if (this.workflow[i].id === id) {
            return i;
          }
        }
        return -1;
      },

      onSuccessorDrop: function(predecessorId, action, nodeInfo) {
        var nodeInstance;
        if (action === 'move') {
          nodeInstance = nodeInfo;
        } else {
          // Create node instance
          nodeInstance = {
            id: uuid(),
            type: nodeInfo.type,
            displayType: nodeInfo.displayType,
            name: nodeInfo.displayType,
          };

          switch (nodeInstance.type) {
          case 'ImageMatching':
            nodeInstance.prop = this.getNewImageMatchingNodeProperties();
            break;

          case 'KeyboardInput':
            nodeInstance.prop = this.getNewKeyboardInputNodeProperties();
            break;

          case 'OpenExplorer':
            nodeInstance.prop = this.getNewOpenExplorerNodeProperties();
            break;

          case 'FileRead':
            nodeInstance.prop = this.getNewFileReadNodeProperties();
            break;

          case 'Wait':
            nodeInstance.prop = this.getNewWaitNodeProperties();
            break;
          }
        }

        if (action === 'move') {
          // Remove node at previous position
          var idx = this.findPositionById(nodeInstance.id);
          this.workflow.splice(idx, 1);
        }

        // Add to workflow
        var i = this.findPositionById(predecessorId);
        this.workflow.splice(i + 1, 0, nodeInstance);

        setTimeout(function() {
          // Emit after new node created
          bus.$emit('workflow-canvas.selectnode', nodeInstance.id);
        });

        return false;
      },

      getNewImageMatchingNodeProperties: function() {
        return {
          imageUrlPath: "",
          startPos: [0, 0],
          endPos: [0, 0],
          windowTitle: "",
          action: "Nothing",
          actionPos: [0, 0],
          timeout: 10,
        };
      },

      getNewKeyboardInputNodeProperties: function() {
        return {
          keys: "",
        };
      },

      getNewOpenExplorerNodeProperties: function() {
        return {
          path: "",
        };
      },

      getNewFileReadNodeProperties: function() {
        return {
          path: "",
        };
      },

      getNewWaitNodeProperties: function() {
        return {
          timeout: 0,
        };
      },

      onNodePropertyChange: function(e) {
        for (var i = 0; i < this.workflow.length; i++) {
          let node = this.workflow[i];

          if (node.id === e.id) {
            node.name = e.name;
            node.prop = e.prop;
            break;
          }
        }
      },

      saveWorkflow: function(callback) {
        callback = callback || function() {};

        var self = this;

        var xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function() {
          if (this.readyState === 4) {
            self.isSaving = false;

            if (this.status === 200) {
              var workflow = JSON.parse(this.response);

              console.log("Saved: id=" + workflow.id);

              callback(workflow);
            } else {
              console.log("Server returned status=" + this.status);

              callback(null, new Error("Failed to save workflow"));
            }
          }
        };

        xhr.onerror = function(err) {
          console.log(err);

          self.isSaving = false;

          callback(null, err);
        };

        if (this.id) {
          // Update
          xhr.open('PATCH', '/workflows/' + this.id);
        } else {
          // Create
          xhr.open('POST', '/workflows');
        }

        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.send(JSON.stringify({
          name: this.name,
          data: this.workflow,
        }));

        this.isSaving = true;
      },

      onSaveButtonClick: function() {
        var self = this;

        this.saveWorkflow(function(resp, err) {
          if (err) {
            alert(err);
            return;
          }

          self.id = resp.id;

          bus.$emit('workflow-canvas.updateworkflows');
        });
      },

      onExecuteButtonClick: function() {
        this.saveWorkflow(function(resp, err) {
          if (err) {
            alert(err);
            return;
          }

          location.href = 'openrpa:execute/' + resp.id;
        });
      },
    },
  });

  Vue.component('rpa-node-property-panel', {
    template: '#tmpl-node-property-panel',

    props: ['active'],

    data: function() {
      return {
        nodeInstance: null,
      };
    },

    computed: {
      nodeType: function() {
        return (this.nodeInstance || {}).type;
      },
    },

    methods: {
      activate: function(nodeInstance) {
        var self = this;

        // For (maybe) bug of mdc-select, set null to nodeInstance
        // before set actual instance.
        this.nodeInstance = null;
        setTimeout(function() {
          self.nodeInstance = nodeInstance;
        });
      },

      inactivate: function() {
        this.nodeInstance = null;
      },
    },
  });

  Vue.component('rpa-image-matching-node-property', {
    template: '#tmpl-image-matching-node-property',

    props: ['nodeInstance'],

    computed: {
      hasImage: function() {
        return this.nodeInstance.prop.imageUrlPath !== "";
      },

      hasRect: function() {
        return this.nodeInstance.prop.startPos[0] !== 0 &&
          this.nodeInstance.prop.startPos[1] !== 0 &&
          this.nodeInstance.prop.endPos[0] !== 0 &&
          this.nodeInstance.prop.endPos[1] !== 0;
      },
    },

    methods: {
      onCaptureButtonClick: function() {
        var self = this;
        var socket = io.connect("/screenshot");

        // TODO: socket error handling
        socket.on('connect', function() {
          console.log('connected.');
        });

        var capturing = false;

        socket.on('receiving screenshot ready', function() {
          if (capturing) {
            return;
          }

          // TODO: get from cookie
          var sessionID = document.querySelector('#session-id').getAttribute('data-session-id');

          // Launch local capture application
          // (Not use location.href for Chrome/IE support)
          var iframe = document.createElement('iframe');
          iframe.style.display = 'none';
          iframe.src = 'openrpa:capture/' + sessionID;
          document.body.appendChild(iframe);

          capturing = true;
        });

        socket.on('receive screenshot', function(data) {
          capturing = false;

          self.nodeInstance.prop.imageUrlPath = data.path;
          self.nodeInstance.prop.windowTitle = data.title;

          socket.close();

          self.$refs.captureImageDialog.show();
        });
      },

      onCaptureImageClick: function() {
        this.$refs.captureImageDialog.show();
      },
    },
  });

  Vue.component('rpa-image-matching-capture-image-dialog', {
    template: '#tmpl-image-matching-capture-image-dialog',

    props: ['nodeInstance'],

    data: function() {
      return {
        imageUrlPath: "",
        startPos: null,
        endPos: null,
        actionPos: null,
      };
    },

    methods: {
      show: function() {
        var prop = JSON.parse(JSON.stringify(this.nodeInstance.prop));
        this.imageUrlPath = prop.imageUrlPath;
        this.startPos = prop.startPos;
        this.endPos = prop.endPos;
        this.actionPos = prop.actionPos;

        // Use setTimeout for propagating data change
        var self = this;
        setTimeout(function() {
          self.$refs.canvas.initialize();
          self.$refs.dialog.show();
        });
      },

      onSave: function() {
        this.nodeInstance.prop.startPos = this.startPos;
        this.nodeInstance.prop.endPos = this.endPos;
        this.nodeInstance.prop.actionPos = this.actionPos;
      },

      onCancel: function() {
      },
    },
  });

  Vue.component('rpa-image-matching-capture-image-dialog-canvas', {
    template: '#tmpl-image-matching-capture-image-dialog-canvas',

    props: ['imageUrlPath', 'startPos', 'endPos', 'actionPos'],

    data: function() {
      return {
        isMouseDown: false,
        isMovingActionPos: false,
        actionPosRadius: 5,
      };
    },

    methods: {
      getTopLeftPosOfRect: function() {
        return [Math.min(this.startPos[0], this.endPos[0]),
                Math.min(this.startPos[1], this.endPos[1])];
      },

      getAbsoluteActionPos: function() {
        var topLeftPos = this.getTopLeftPosOfRect();
        return [topLeftPos[0] + this.actionPos[0],
                topLeftPos[1] + this.actionPos[1]];
      },

      initialize: function() {
        this.isMouseDown = false;
        this.draw();
      },

      draw: function() {
        var self = this;
        var ctx = this.$refs.canvas.getContext('2d');
        var img = new Image();

        img.onload = function() {
          self.$refs.canvas.width = img.width;
          self.$refs.canvas.height = img.height;
          ctx.drawImage(img, 0, 0);

          self.drawRect(ctx);
          self.drawPoint(ctx);
        }
        img.src = this.imageUrlPath;
      },

      drawRect: function(ctx) {
        var startPos = this.startPos;
        var endPos = this.endPos;

        if (startPos[0] === endPos[0] &&
            startPos[1] === endPos[1]) {
          return;
        }

        ctx.save();

        // Set line style
        ctx.strokeStyle = "#00ff00";
        ctx.lineWidth = 5;
        ctx.setLineDash([2, 3]);

        ctx.beginPath();

        // Top
        ctx.moveTo(startPos[0], startPos[1]);
        ctx.lineTo(endPos[0], startPos[1]);

        // Bottom
        ctx.moveTo(startPos[0],endPos[1]);
        ctx.lineTo(endPos[0],endPos[1]);

        // Right
        ctx.moveTo(endPos[0],startPos[1]);
        ctx.lineTo(endPos[0],endPos[1]);

        // Left
        ctx.moveTo(startPos[0],startPos[1]);
        ctx.lineTo(startPos[0],endPos[1]);

        ctx.stroke();

        ctx.restore();
      },

      drawPoint: function(ctx) {
        var startPos = this.startPos;
        var endPos = this.endPos;

        if (startPos[0] === endPos[0] &&
            startPos[1] === endPos[1]) {
          return;
        }

        ctx.save();

        // Set point style
        ctx.strokeStyle = ctx.fillStyle = "#00ff00";

        // Draw point (small filled circle)
        ctx.beginPath();
        var absPos = this.getAbsoluteActionPos();
        ctx.arc(absPos[0], absPos[1], this.actionPosRadius, 0, 2 * Math.PI);
        ctx.fill();
        ctx.stroke();

        ctx.restore();
      },

      onMouseDown: function(e) {
        this.isMouseDown = true;

        var rect = e.target.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;

        // If cursor is on actionPos...
        var absPos = this.getAbsoluteActionPos();
        if (Math.pow(x - absPos[0], 2) + Math.pow(y - absPos[1], 2) <=
            Math.pow(this.actionPosRadius, 2)) {
          // Start moving actionPos
          this.isMovingActionPos = true;

          return;
        }

        this.startPos[0] = x;
        this.startPos[1] = y;
      },

      onMouseMove: function(e) {
        if (!this.isMouseDown) {
          return;
        }

        var rect = e.target.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;

        var topLeftPos = this.getTopLeftPosOfRect();

        if (this.isMovingActionPos) {
          // Set actionPos to cursor position
          this.actionPos[0] = x - topLeftPos[0];
          this.actionPos[1] = y - topLeftPos[1];
        } else {
          // Set endPos to cursor position
          this.endPos[0] = x;
          this.endPos[1] = y;

          // Set actionPos to center of rect
          this.actionPos[0] =
            (this.startPos[0] + this.endPos[0]) / 2 - topLeftPos[0];
          this.actionPos[1] =
            (this.startPos[1] + this.endPos[1]) / 2 - topLeftPos[1];
        }

        this.draw();
      },

      onMouseUp: function() {
        this.isMouseDown = false;
        this.isMovingActionPos = false;
      },
    },
  });

  Vue.component('rpa-keyboard-input-node-property', {
    template: '#tmpl-keyboard-input-node-property',

    props: ['nodeInstance'],
  });

  Vue.component('rpa-open-explorer-node-property', {
    template: '#tmpl-open-explorer-node-property',

    props: ['nodeInstance'],
  });

  Vue.component('rpa-file-read-node-property', {
    template: '#tmpl-file-read-node-property',

    props: ['nodeInstance'],
  });

  Vue.component('rpa-wait-node-property', {
    template: '#tmpl-wait-node-property',

    props: ['nodeInstance'],
  });

  Vue.component('rpa-workflow-list', {
    template: '#tmpl-workflow-list',

    data: function() {
      return {
        workflows: [],
      };
    },

    created: function() {
      var self = this;

      bus.$on('workflow-canvas.updateworkflows', function() {
        self.reloadWorkflows();
      });
    },

    mounted: function() {
      this.reloadWorkflows();
    },

    methods: {
      reloadWorkflows: function() {
        var self = this;

        var xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function() {
          if (this.readyState === 4) {
            if (this.status === 200) {
              var workflows = JSON.parse(this.response);

              self.workflows.splice(0, self.workflows.length);
              self.workflows = self.workflows.concat(workflows);
            } else {
              console.log("Failed to fetch workflows: status=" + this.status);
            }
          }
        };

        xhr.onerror = function(err) {
          coonsole.log("Failed to fetch workflows", err);
        };

        xhr.open('GET', '/workflows');
        xhr.send();
      },

      onItemClick: function(e) {
        var id = e.target.getAttribute('data-id');

        var self = this;

        var xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function() {
          if (this.readyState === 4) {
            if (this.status === 200) {
              var workflow = JSON.parse(this.response);

              bus.$emit('workflow-list.selectworkflow', workflow);
            } else {
              console.log("Failed to fetch workflow: status=" + this.status);
            }
          }
        };

        xhr.onerror = function(err) {
          coonsole.log("Failed to fetch workflow", err);
        };

        xhr.open('GET', '/workflows/' + id);
        xhr.send();
      },
    },
  });

  new Vue({
    el: '#app',

    created: function() {
      var self = this;
      bus.$on('node-instance.select', function(nodeInstance) {
        self.activateNodePropertyPanel(nodeInstance);
      });

      bus.$on('workflow-list.selectworkflow', function(workflow) {
        self.inactivateNodePropertyPanel();
      });
    },

    data: {
      isNodePropertyPanelActive: false,
    },

    methods: {
      activateNodePropertyPanel: function(nodeInstance) {
        this.isNodePropertyPanelActive = true;
        this.$refs.nodePropertyPanel.activate(nodeInstance);
      },

      inactivateNodePropertyPanel: function() {
        this.isNodePropertyPanelActive = false;
        this.$refs.nodePropertyPanel.inactivate();
      },

      onTitleClick: function() {
        location.href = '/';
      },
    },
  });
};
