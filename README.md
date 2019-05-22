# Horisontal Pods Autoscaling for .NET applications

## Benefits of Autoscaling

To understand better where autoscaling would provide the most value, let’s start with an example. Imagine you have a 24/7 production service with a load that is variable in time, where it is very busy during the day in the US, and relatively low at night. Ideally, we would want the number of nodes in the cluster and the number of pods in deployment to dynamically adjust to the load to meet end user demand. The new Cluster Autoscaling feature together with Horizontal Pod Autoscaler can handle this for you automatically.

## How does the Horizontal Pod Autoscaler work?

![HPA](https://d33wubrfki0l68.cloudfront.net/4fe1ef7265a93f5f564bd3fbb0269ebd10b73b4e/1775d/images/docs/horizontal-pod-autoscaler.svg)

The Horizontal Pod Autoscaler is implemented as a control loop, with a period controlled by the controller manager’s --horizontal-pod-autoscaler-sync-period flag (with a default value of 15 seconds).

During each period, the controller manager queries the resource utilization against the metrics specified in each HorizontalPodAutoscaler definition. The controller manager obtains the metrics from either the resource metrics API (for per-pod resource metrics), or the custom metrics API (for all other metrics).

For per-pod resource metrics (like CPU), the controller fetches the metrics from the resource metrics API for each pod targeted by the HorizontalPodAutoscaler. Then, if a target utilization value is set, the controller calculates the utilization value as a percentage of the equivalent resource request on the containers in each pod. If a target raw value is set, the raw metric values are used directly. The controller then takes the mean of the utilization or the raw value (depending on the type of target specified) across all targeted pods, and produces a ratio used to scale the number of desired replicas.

More Details can be found here: [https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)

## Horizontal Pod Autoscaler base commands

Horizontal Pod Autoscaler, like every API resource, is supported in a standard way by `kubectl`. We can create a new autoscaler using `kubectl create` command. We can list autoscalers by `kubectl get hpa` and get detailed description by `kubectl describe hpa`. Finally, we can delete an autoscaler using `kubectl delete hpa`.

In addition, there is a special `kubectl autoscale` command for easy creation of a Horizontal Pod Autoscaler. For instance, executing `kubectl autoscale rs foo --min=2 --max=5 --cpu-percent=80` will create an autoscaler for replication set foo, with target CPU utilization set to 80% and the number of replicas between 2 and 5. 

The full list of possible autoscale commands can be found here: [https://kubernetes.io/docs/reference/generated/kubectl/kubectl-commands#autoscale](https://kubernetes.io/docs/reference/generated/kubectl/kubectl-commands#autoscale)

## Horizontal Pod Autoscale with custom metrics

By default, the HorizontalPodAutoscaler controller retrieves metrics from a series of APIs. In order for it to access these APIs, cluster administrators must ensure that:

1. The API aggregation layer is enabled. Configuring the aggregation layer allows the Kubernetes apiserver to be extended with additional APIs, which are not part of the core Kubernetes APIs.

1. The corresponding APIs are registered:

    * For resource metrics, this is the `metrics.k8s.io` API, generally provided by metrics-server. It can be launched as a cluster addon.

    * For custom metrics, this is the `custom.metrics.k8s.io` API. **It’s provided by “adapter” API servers provided by metrics solution vendors.** Check with your metrics pipeline, or the list of known solutions. If you would like to write your own, check out the boilerplate to get started.

    * For external metrics, this is the `external.metrics.k8s.io` API. It may be provided by the custom metrics adapters provided above.

    * The `--horizontal-pod-autoscaler-use-rest-clients` is `true` or `unset`. Setting this to false switches to Heapster-based autoscaling, which is deprecated.
  
More information is here: [https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/#support-for-metrics-apis](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/#support-for-metrics-apis)

## Custom metrics architecture high level

This architecture includes the **prometheus custom metrics adapter**, which is used to extend the `custom.metrics.k8s.io` API with your own metrics.

![CM](https://github.com/luxas/kubeadm-workshop/blob/master/pictures/custom-metrics-architecture.png)
